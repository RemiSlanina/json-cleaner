using Xunit; 
using CleanJsonApp.Services; 
using System.Text.Json.Nodes;

namespace CleanJsonApp.Tests;

public class UnitTest1
{
    // just testing if tests are working: Passed:  2 
    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }

    [Fact]
    public void RemoveEmptyStrings_Removes_Empty_String_Properties()
    {
        // arrange
        JsonProcessor processor = new();

        JsonNode json = JsonNode.Parse("""
        {
            "name": ""
        }
        """)!;

        List<JsonProcessor.CleanAction> actions =
        [
            JsonProcessor.CleanAction.RemoveEmptyStrings
        ];

        // act
        JsonNode? result = processor.ProcessJson(
            json,
            JsonProcessor.KeyActionOption.None,
            actions
        );

        // assert
        Assert.DoesNotContain("name", result!.ToJsonString());
    }
}