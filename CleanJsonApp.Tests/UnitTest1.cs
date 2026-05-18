using Xunit; 
using CleanJsonApp.Services; 
using System.Text.Json.Nodes;
using System.Linq; 
using System.Diagnostics;

namespace CleanJsonApp.Tests;

public class UnitTest1
{
    // just testing if tests are working: Passed 6 
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

        // parse json using raw string literals
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

    
    [Fact] 
    public void TrimWhitespace_Trims_String_Value()
    {
        // arrange 
        JsonProcessor processor = new(); 

        // parse json using raw string literals 
        JsonNode json = JsonNode.Parse("""
        {
            "name": "     Lui   "
        }
        """)!;

        List<JsonProcessor.CleanAction> actions = [JsonProcessor.CleanAction.TrimWhitespace]; 
        
        // act 
        JsonNode? result = processor.ProcessJson(
            json, JsonProcessor.KeyActionOption.None, actions
        );

        // assert 
        Assert.Equal("Lui", result!["name"]!.ToString()); 
    }

    [Fact]
    public void TrimWhitespace_RemoveEmptyStrings_Whitespace_Only_Removal()
    {
        // arrange 
        JsonProcessor processor = new(); 

        // parse json using raw string literals 
        JsonNode json = JsonNode.Parse("""
        {
            "name": "        "
        }
        """)!;

        List<JsonProcessor.CleanAction> actions = [
            JsonProcessor.CleanAction.TrimWhitespace, 
            JsonProcessor.CleanAction.RemoveEmptyStrings
            ]; 
        
        // act 
        JsonNode? result = processor.ProcessJson(
            json, JsonProcessor.KeyActionOption.None, actions
        );

        // assert 
        Assert.Null(result!["name"]); 
    }

     [Fact]
    public void RemoveEmptyKeys_Removes_Null_Property()
    {
        // arrange 
        JsonProcessor processor = new(); 

        // parse json using raw string literals 
        JsonNode json = JsonNode.Parse("""
        {
            "name": null
        }
        """)!;

        List<JsonProcessor.CleanAction> actions = []; 
        
        // act 
        JsonNode? result = processor.ProcessJson(
            json, JsonProcessor.KeyActionOption.RemoveEmptyKeys, actions
        );

        // assert 
        Assert.Null(result!["name"]);
    }

    // switching to helper function.. 
    private static JsonNode RunCleaner(string str, JsonProcessor.KeyActionOption mode, params JsonProcessor.CleanAction[] cleaners)
    { // params ~ like args in javaScript ...
        // arrange 
        JsonProcessor processor = new(); 
        JsonNode json = JsonNode.Parse(str)!;  
        // act 
        return processor.ProcessJson(
            json, mode, cleaners.ToList()
        )!; 
    }
    [Fact] 
    public void FillMissingKeys_Fills_Two_Missing_Keys()
    {
        // arrange and act inside RunCleaner:
        JsonNode? result = RunCleaner(
            """
                [
                    {
                        "name": "George",
                        "active": true
                    },
                    {
                        "name": "Pete",
                        "active": true
                    },
                    {
                        "active": true
                    }
                ]
            """, JsonProcessor.KeyActionOption.FillMissingKeys
        );

        // assert 
        // would be true when property contains null, or property does not exist
        // Assert.Null(result![2]!["name"]); 
        // so better in 2 parts actually 
        JsonObject thirdObject = result![2]!.AsObject(); 
        Assert.True(thirdObject.ContainsKey("name")); 
        Assert.Null(thirdObject["name"]); 
    }


}