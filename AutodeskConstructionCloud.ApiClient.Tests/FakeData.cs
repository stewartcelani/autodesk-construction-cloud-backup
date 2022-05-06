﻿using System;
using System.Collections.Generic;
using Bogus;
using AutodeskConstructionCloud.ApiClient.Entities;

namespace AutodeskConstructionCloud.ApiClient.Tests;

public static class FakeData
{
    
    public static List<Folder> GetFakeFolders(int numberOfFolders, ApiClient apiClient)
    {
        var folderList = new List<Folder>();
        for (int i = 0; i < numberOfFolders; i++)
        {
            folderList.Add(GetFakeFolder(apiClient));
        }

        return folderList;
    }

    public static Folder GetFakeFolder(ApiClient apiClient)
    {
        var faker = new Faker();
        return new Folder(apiClient)
        {
            FolderId = Guid.NewGuid().ToString(),
            ProjectId = Guid.NewGuid().ToString(),
            ParentFolderId = Guid.NewGuid().ToString(),
            ParentFolder = null,
            Name = faker.Company.CompanyName(),
            Type = "Folder",
            CreateTime = DateTime.Now,
            CreateUserId = Guid.NewGuid().ToString(),
            CreateUserName = faker.Internet.UserName(),
            DisplayName = faker.Company.CompanyName(),
            Hidden = false,
            LastModifiedTime = DateTime.Now,
            LastModifiedTimeRollup = DateTime.Now,
            LastModifiedUserId = Guid.NewGuid().ToString(),
            LastModifiedUserName = faker.Internet.UserName(),
            ObjectCount = faker.Random.Number(0, 10)
        };
    }
    
    public static File GetFakeFile()
    {
        var faker = new Faker();
        return new File()
        {
            FileId = Guid.NewGuid().ToString(),
            ProjectId = Guid.NewGuid().ToString(),
            ParentFolder = null,
            Name = faker.Company.CompanyName(),
            Type = "File",
            VersionNumber = 1,
            DisplayName = string.Empty,
            CreateTime = DateTime.Now,
            CreateUserId = Guid.NewGuid().ToString(),
            CreateUserName = faker.Internet.UserName(),
            Hidden = false,
            LastModifiedTime = DateTime.Now,
            LastModifiedUserId = Guid.NewGuid().ToString(),
            LastModifiedUserName = faker.Internet.UserName(),
            DownloadUrl = faker.Internet.Url(),
            Reserved = false,
            ReservedTime = DateTime.Now,
            ReservedUserId = Guid.NewGuid().ToString(),
            ReservedUserName = faker.Internet.UserName()
        };
    }
}
