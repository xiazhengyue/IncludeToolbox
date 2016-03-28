#include "IncludeParser.h"

#include <Foundation/Configuration/Startup.h>
#include <Foundation/IO/FileSystem/DataDirTypeFolder.h>
#include <Foundation/IO/FileSystem/FileReader.h>
#include <Foundation/Containers/HashTable.h>
#include <Foundation/Memory/MemoryUtils.h>

namespace
{
	class StringStorage
	{
	public:
		static StringHandle GetNewHandle()
		{
			StringHandle outHandle = nextHandle;
			cachedStrings.Insert(outHandle, ezString());
			++nextHandle;
			return outHandle;
		}

		static ezResult GetString(StringHandle handle, ezString*& outString)
		{
			return cachedStrings.TryGetValue(handle, outString) ? EZ_SUCCESS : EZ_FAILURE;
		}

		static void RemoveString(StringHandle handle)
		{
			cachedStrings.Remove(handle);
		}

	private:
		static StringHandle nextHandle;
		static ezHashTable<StringHandle, ezString> cachedStrings;
	};

	StringHandle StringStorage::nextHandle = 0;
	ezHashTable<StringHandle, ezString> StringStorage::cachedStrings;
}


void __stdcall Init()
{
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
	ezString* string;
	if (StringStorage::GetString(handle, string).Failed())
		return RESULT_FAILURE;

	int32_t copySize = ezMath::Min<int32_t>(bufferSize, string->GetElementCount() + 1);
	ezMemoryUtils::Copy(buffer, string->GetData(), copySize);

	StringStorage::RemoveString(handle);

	return RESULT_SUCCESS;
}

Result __stdcall GetStringLength(StringHandle handle, int32_t* outBufferSize)
{
	ezString* string;
	if (StringStorage::GetString(handle, string).Failed())
		return RESULT_FAILURE;

	*outBufferSize = string->GetElementCount() + 1;

	return RESULT_SUCCESS;
}

Result __stdcall Test(const char* absoluteIncludeFilename, StringHandle* outString)
{
	ezFileReader fileReader;
	if (fileReader.Open(absoluteIncludeFilename).Failed())
		return RESULT_FAILURE;

	ezString* fileContent;
	*outString = StringStorage::GetNewHandle();
	StringStorage::GetString(*outString, fileContent);

	fileContent->ReadAll(fileReader);

	return RESULT_SUCCESS;
}