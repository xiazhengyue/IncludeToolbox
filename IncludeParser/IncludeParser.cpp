#include <Foundation/Configuration/Startup.h>
#include <Foundation/IO/FileSystem/DataDirTypeFolder.h>
#include <Foundation/IO/FileSystem/FileReader.h>
#include <Foundation/Containers/HashTable.h>
#include <Foundation/Memory/MemoryUtils.h>
#include <Foundation/Logging/VisualStudioWriter.h>
#include <Foundation/Logging/ConsoleWriter.h>

#include <CoreUtils/CodeUtils/Preprocessor.h>

#include "IncludeParser.h"


namespace
{
	class StringStorage
	{
	public:
		static StringHandle GetNewHandle(ezStringBuilder*& outString)
		{
			StringHandle outHandle = nextHandle;
			cachedStrings.Insert(outHandle, ezString());
			++nextHandle;
			GetString(outHandle, outString);
			return outHandle;
		}

		static ezResult GetString(StringHandle handle, ezStringBuilder*& outString)
		{
			return cachedStrings.TryGetValue(handle, outString) ? EZ_SUCCESS : EZ_FAILURE;
		}

		static void RemoveString(StringHandle handle)
		{
			cachedStrings.Remove(handle);
		}

	private:
		static StringHandle nextHandle;
		static ezHashTable<StringHandle, ezStringBuilder> cachedStrings;
	};

	StringHandle StringStorage::nextHandle = 0;
	ezHashTable<StringHandle, ezStringBuilder> StringStorage::cachedStrings;
}


void __stdcall Init()
{
#ifdef _DEBUG
	ezGlobalLog::AddLogWriter(ezLogWriter::Console::LogMessageHandler);
	ezGlobalLog::AddLogWriter(ezLogWriter::VisualStudio::LogMessageHandler);
#endif

	ezStartup::StartupCore();
	ezFileSystem::RegisterDataDirectoryFactory(ezDataDirectory::FolderType::Factory);
	ezFileSystem::AddDataDirectory("");
}
void __stdcall Exit()
{
	ezStartup::ShutdownCore();
}

Result __stdcall ResolveString(StringHandle handle, char* buffer, int32_t bufferSize)
{
	ezStringBuilder* string;
	if (StringStorage::GetString(handle, string).Failed())
		return RESULT_FAILURE;

	int32_t copySize = ezMath::Min<int32_t>(bufferSize, string->GetElementCount() + 1);
	ezMemoryUtils::Copy(buffer, string->GetData(), copySize);

	StringStorage::RemoveString(handle);

	return RESULT_SUCCESS;
}

Result __stdcall GetStringLength(StringHandle handle, int32_t* outBufferSize)
{
	ezStringBuilder* string;
	if (StringStorage::GetString(handle, string).Failed())
		return RESULT_FAILURE;

	*outBufferSize = string->GetElementCount() + 1;

	return RESULT_SUCCESS;
}

namespace
{
	class FileLocator
	{
	public:
		FileLocator(const char* includeDirectories, ezStringBuilder* outIncludeTreeString)
		{
			ezStringBuilder(includeDirectories).Split(false, includeDirectoryList, ";");
			for (ezStringBuilder& dir : includeDirectoryList)
				dir.MakeCleanPath();


			this->outIncludeTreeString = outIncludeTreeString;
		}

		ezResult operator () (const char* szCurAbsoluteFile, const char* szIncludeFile, ezPreprocessor::IncludeType IncType, ezString& out_sAbsoluteFilePath)
		{
			// As of now, ezPreprocessor will always run the file locator, even if the file in question is already cached.
			// This makes it the perfect place to build up the include tree in which we are interested here.
			// Also, it is known that this process is recursive, so we can fill up the stack with whatever file we're opening.

			// Go back to the file where this include was found.
			int nestingDepth = fileStack.GetCount() - 1;
			for (; nestingDepth >= 0; --nestingDepth)
			{
				if (fileStack[nestingDepth].Compare(szCurAbsoluteFile) == 0)
					break;
			}
			fileStack.SetCount(nestingDepth + 1);

			ezResult result = resolveFile(szCurAbsoluteFile, szIncludeFile, IncType, out_sAbsoluteFilePath);
			if (IncType == ezPreprocessor::MainFile)
				return result;

			// Make entry in tree string with appropriate depth.
			for (int i = 0; i < (int)fileStack.GetCount(); ++i)
				outIncludeTreeString->Append('\t');
			if(result.Succeeded())
			{
				fileStack.PushBack(out_sAbsoluteFilePath);
				outIncludeTreeString->Append(out_sAbsoluteFilePath, "\n");
			}
			else
			{
				outIncludeTreeString->Append(szIncludeFile, " <not found!>\n");
			}

			return result;
		}

	private:
		ezResult resolveFile(const char* szCurAbsoluteFile, const char* szIncludeFile, ezPreprocessor::IncludeType IncType, ezString& out_sAbsoluteFilePath)
		{
			// See http://stackoverflow.com/questions/4118376/what-are-the-rules-on-include-xxx-h-vs-include-xxx-h

			// Try absolute.
			if (ezFileSystem::ExistsFile(szIncludeFile))
			{
				out_sAbsoluteFilePath = szIncludeFile;
				return EZ_SUCCESS;
			}

			// Try relative.
			ezStringBuilder s = szCurAbsoluteFile;
			s.PathParentDirectory();
			s.AppendPath(szIncludeFile);
			if (ezFileSystem::ExistsFile(s.GetData()))
			{
				out_sAbsoluteFilePath = s;
				return EZ_SUCCESS;
			}

			// Try search directories.
			for (const ezStringBuilder& dir : includeDirectoryList)
			{
				s = dir;
				s.AppendPath(szIncludeFile);
				if (ezFileSystem::ExistsFile(s.GetData()))
				{
					out_sAbsoluteFilePath = s;
					return EZ_SUCCESS;
				}
			}

			return EZ_FAILURE;
		}

		ezDynamicArray<ezStringBuilder> includeDirectoryList;
		ezDynamicArray<ezString> fileStack;
		ezStringBuilder* outIncludeTreeString;
	};
}

Result __stdcall ParseIncludes(const char* inputFilename, const char* includeDirectories, const char* preprocessorDefinitions,
								StringHandle* outProcessedInputFile, StringHandle* outIncludeTree)
{
	ezPreprocessor preprocessor;

	// Setup preprocessor defines.
	{
		ezDynamicArray<ezString> preprocessorDefinitionsArray;
		ezStringBuilder(preprocessorDefinitions).Split(false, preprocessorDefinitionsArray, ";");
		for (const ezString& def : preprocessorDefinitionsArray)
			preprocessor.AddCustomDefine(def);
	}

	// Prepare file locator.
	ezStringBuilder* outIncludeTreeString;
	*outIncludeTree = StringStorage::GetNewHandle(outIncludeTreeString);
	FileLocator fileLocator(includeDirectories, outIncludeTreeString);

	// Our file locator is too large for ezDelegate, so we need to wrap it.
	preprocessor.SetFileLocatorFunction([&fileLocator](const char* szCurAbsoluteFile, const char* szIncludeFile, ezPreprocessor::IncludeType IncType, ezString& out_sAbsoluteFilePath)
	{
		return fileLocator(szCurAbsoluteFile, szIncludeFile, IncType, out_sAbsoluteFilePath);
	});

	// Setup logging.
	Result result = RESULT_SUCCESS;
	preprocessor.m_ProcessingEvents.AddEventHandler([&result](const ezPreprocessor::ProcessingEvent& event)
	{
		if (event.m_Type == ezPreprocessor::ProcessingEvent::Error)
		{
			//if(ezStringUtils::FindSubString(event.m_szInfo, "Redefinition of macro") == nullptr)
			result = RESULT_FAILURE;
		}
	});
#ifdef _DEBUG
	preprocessor.SetLogInterface(ezLog::GetDefaultLogSystem());
#endif
	// todo (?): Output the log to the user?

	// Process.
	ezStringBuilder* processedFile;
	*outProcessedInputFile = StringStorage::GetNewHandle(processedFile);
	preprocessor.Process(inputFilename, *processedFile);

	return result;
}