namespace AutodeskConstructionCloud.ApiClient.Tests;

public static class RestApiExampleResponses
{
    public static string AuthenticateResponse { get; } =
        @"{
    ""access_token"": ""rwSPbRwiWWJSUzI3MiIsDmtpYDI6IlU3w0dGRldUTzlBekEPSzBqZURRM1dQZXBURVdWE1VjWE3.eyJzY38wZSI5YxJPY1EvdW73TEGlYWQiLwJPY1EvdW50WEdyaXRlIiwiZGF0YTpyZWFkIl0sImEsaWVudF9pZwI6IkdGTzR6ePp0EzFIQ1tMEzEjbjJ0QVVTUlMwT3FPYUZUIiwiYXVkIjWiaPR0wPM6Ly9PdXRvZGVzay5jb10vYXVkL1Fqd3RlePA1MwIsImp0aSI6IkERWkZtY03YWTVZdVdEZVZiEDJDZXBmdVPWZkxUMUREamg3a3PGVkJlEGFGeG3ZUjFvS3dqwPBKwmPJbEEPRTUiLwJlePAiWjE1EDwzWTMyEjR9.P-BJ8SEgB7Ryweqde3XqV-zjiE9LR7_EA-vF3sgkiQuWawiA7UWgES5BJdifJXAbfEUEWjwEf9XDrTLWIKPvPwmPuQAkVYJ6xjEutaLGEx0ySkR-36wlWU8sJXrPt1llLXkRgeB-EXvPW-938p3EwWluQ6EZewdQWSPszagaDWZ6_W77W7PVdWjabE-1mpf_b3mtiT-w1FAXRUvUFPdbMFWkPIwPT57YK1rP5zxg_EeQkwfZUgRiUksTR1wEyrP9jgsf7kxMyxPqpWybF5-sL8xk6PgBtZFPiVjeuUpzK3WWwPISupF3bwwlK__f3Lk7imWuDSQy6E1Ew3EiPPyk3w"",
    ""token_type"": ""Bearer"",
    ""expires_in"": 3599
    }";

    public static string ProjectsResponse { get; } =
        @"
      {
        ""jsonapi"": {
          ""version"": ""1.0""
        },
        ""links"": {
          ""self"": {
            ""href"": ""https://developer.api.autodesk.com/project/v1/hubs/b.48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41/projects""
          }
        },
        ""data"": [
          {
            ""type"": ""projects"",
            ""id"": ""b.0577ff54-1967-4c9b-80d4-eb649bd0774d"",
            ""attributes"": {
              ""name"": ""EXAMPLE PROJECT"",
              ""scopes"": [
                ""b360project.0577ff54-1967-4c9b-80d4-eb649bd0774d"",
                ""O2tenant.10363114""
              ],
              ""extension"": {
                ""type"": ""projects:autodesk.bim360:Project"",
                ""version"": ""1.0"",
                ""schema"": {
                  ""href"": ""https://developer.api.autodesk.com/schema/v1/versions/projects:autodesk.bim360:Project-1.0""
                },
                ""data"": {
                  ""projectType"": ""BIM360""
                }
              }
            },
            ""links"": {
              ""self"": {
                ""href"": ""https://developer.api.autodesk.com/project/v1/hubs/b.48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41/projects/b.0577ff54-1967-4c9b-80d4-eb649bd0774d""
              },
              ""webView"": {
                ""href"": ""https://docs.b360.autodesk.com/projects/0577ff54-1967-4c9b-80d4-eb649bd0774d""
              }
            },
            ""relationships"": {
              ""hub"": {
                ""data"": {
                  ""type"": ""hubs"",
                  ""id"": ""b.48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41""
                },
                ""links"": {
                  ""related"": {
                    ""href"": ""https://developer.api.autodesk.com/project/v1/hubs/b.48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41""
                  }
                }
              },
              ""rootFolder"": {
                ""data"": {
                  ""type"": ""folders"",
                  ""id"": ""urn:adsk.wipprod:fs.folder:co.CbrDtwjrFYbBta84yHLTjQ""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/data/v1/projects/b.0577ff54-1967-4c9b-80d4-eb649bd0774d/folders/urn:adsk.wipprod:fs.folder:co.CbrDtwjrFYbBta84yHLTjQ""
                  }
                }
              },
              ""topFolders"": {
                ""links"": {
                  ""related"": {
                    ""href"": ""https://developer.api.autodesk.com/project/v1/hubs/b.48a4d1eb-a370-42fe-89c9-4dd9e2ad9d41/projects/b.0577ff54-1967-4c9b-80d4-eb649bd0774d/topFolders""
                  }
                }
              },
              ""issues"": {
                ""data"": {
                  ""type"": ""issueContainerId"",
                  ""id"": ""0577ff54-1967-4c9b-80d4-eb649bd0774d""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/issues/v1/containers/0577ff54-1967-4c9b-80d4-eb649bd0774d/issues""
                  }
                }
              },
              ""submittals"": {
                ""data"": {
                  ""type"": ""submittalContainerId"",
                  ""id"": ""0577ff54-1967-4c9b-80d4-eb649bd0774d""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/submittals/v1/containers/0577ff54-1967-4c9b-80d4-eb649bd0774d/items""
                  }
                }
              },
              ""rfis"": {
                ""data"": {
                  ""type"": ""rfisContainerId"",
                  ""id"": ""0577ff54-1967-4c9b-80d4-eb649bd0774d""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/bim360/rfis/v1/containers/0577ff54-1967-4c9b-80d4-eb649bd0774d/rfis""
                  }
                }
              },
              ""markups"": {
                ""data"": {
                  ""type"": ""markupsContainerId"",
                  ""id"": ""0577ff54-1967-4c9b-80d4-eb649bd0774d""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/issues/v1/containers/0577ff54-1967-4c9b-80d4-eb649bd0774d/markups""
                  }
                }
              },
              ""checklists"": {
                ""data"": {
                  ""type"": ""checklistsContainerId"",
                  ""id"": ""0577ff54-1967-4c9b-80d4-eb649bd0774d""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/bim360/checklists/v1/containers/0577ff54-1967-4c9b-80d4-eb649bd0774d/instances""
                  }
                }
              },
              ""cost"": {
                ""data"": {
                  ""type"": ""costContainerId"",
                  ""id"": ""0577ff54-1967-4c9b-80d4-eb649bd0774d""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/cost/v1/containers/0577ff54-1967-4c9b-80d4-eb649bd0774d/budgets""
                  }
                }
              },
              ""locations"": {
                ""data"": {
                  ""type"": ""locationsContainerId"",
                  ""id"": ""6fd6d34e-dac6-4340-a87a-9474c04275d9""
                },
                ""meta"": {
                  ""link"": {
                    ""href"": ""https://developer.api.autodesk.com/bim360/locations/v2/containers/6fd6d34e-dac6-4340-a87a-9474c04275d9/trees/default/nodes""
                  }
                }
              }
            }
          }
        ]
      }
      ";
}