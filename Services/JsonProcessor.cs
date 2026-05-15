using System.Text.Json; 
using System.Text.Json.Nodes; 

namespace CleanJsonApp.Services; 

public class JsonProcessor
{
    public enum CleanAction
    {
        RemoveEmptyStrings,
        RemoveEmptyArrays,  // this should be its own function/class/enum
        TrimWhitespace,  
        RemoveZeroValues, // TO DO later 
        RemoveTrailingZero // TO DO later 
   
    }
    public enum KeyActionOption 
    {
        None, 
        RemoveEmptyKeys,
        FillMissingKeys
    }
     public class TraverseResult 
    {
        public bool Remove { get; set; } 
        public JsonNode? Value { get; set; }
    }

    public int ChangesMade { get; private set; } 
    public string ErrorMessage { get; private set; } = ""; 
    

    //      @ProcessJson:
    //     Currently calls 3 functions: 
    //     calls RemoveEmptyKeys (old recursive function).
    //     OR FillMissingKeys (mutually exclusive)
    //     then calls: 
    //     calls TraverseTree and this calls CleanJson (performs other cleaning)
        
    //     TraverseTree:
    //     - walk structure
    //     - rebuild structure
    //     - propagate removal decisions upward

    //    CleanJson:
    //     - decide whether primitive node survives 
    //     -----------------------------------------
    //     mutually exclusive: 
    //     MODE (exclusive)
    //     - FillMissingKeys
    //     - RemoveEmptyKeys
    //     - None
        
    //     can be compound: (add radio buttons)
    //     CLEANERS (composable)
    //     - RemoveEmptyStrings
    //     - TrimWhitespace
        
    //     structural (RemoveEmptyObjects, RemoveEmptyArrays) : not currently supported  
        

        // ProcessJson(parsedJson, actionChoice, cleanActions)
    public JsonNode? ProcessJson(JsonNode? json, KeyActionOption? actionChoice, List<JsonProcessor.CleanAction>? cleanActions){

        ChangesMade = 0; // reset counter for each turn 


        // for testing, set cleanActions to a given value: 
        // @* TrimWhitespace needs to come first, because it could create "" eligible for RemoveEmptyStrings *@
        Console.WriteLine("json input: " + JsonSerializer.Serialize(json));
        // @* Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(json));  *@
        Console.WriteLine(); 

        if (json == null) return new JsonObject(); 
        if (cleanActions != null && cleanActions.Count> 0) {
            Console.WriteLine("Calling TraverseTree()");
            TraverseResult? traversed = TraverseTree(json!, cleanActions); 
            if (traversed != null) json = traversed.Value; 
            else Console.WriteLine("Error - TraverseTree returned Null in ProcessJson!");
        } 
        // @* these should come at the end (especially FillMissingKeys for removed ""...) *@
        if (actionChoice == KeyActionOption.RemoveEmptyKeys) { 
            Console.WriteLine("Calling RemoveEmptyKeys()");
            json = RemoveEmptyKeys(json);  
        } 
        if (actionChoice == KeyActionOption.FillMissingKeys) {
            Console.WriteLine("Calling FillMissingKeys()");
            json = FillMissingKeys(json!); // ! = null-forgiving operator 
        } 
        

        return json; 
    } 


    public TraverseResult? CleanJson(JsonNode json, List<CleanAction>? cleanActions) 
        { 
        if (cleanActions == null || cleanActions!.Count == 0) return new TraverseResult{ Remove = false, Value = json }; 

        JsonNode? current = json.DeepClone(); 
        bool remove = false; 

        foreach (CleanAction c in cleanActions) {
            // if already removed, stop processing: 
            if (remove || current == null) {
                break; 
            }

            switch (c) 
            {
                case CleanAction.TrimWhitespace: 
                Console.WriteLine("Trim Whitespace selected"); 
                
                if (current is JsonValue trimValue && trimValue.GetValueKind() == JsonValueKind.String)
                {
                    string original = trimValue.GetValue<string>(); 
                    string trimmed = original.Trim(); 

                    if (original != trimmed)
                    {
                        ChangesMade++; 

                        current = JsonValue.Create(trimmed); 
                    }
                }
                break; 
            
                case CleanAction.RemoveEmptyStrings: 
                Console.WriteLine("Removing empty strings"); 

                if (current is JsonValue stringValue && stringValue.GetValueKind() == JsonValueKind.String)
                {
                    string str = stringValue.GetValue<string>(); 
                    
                    if (str == "") 
                    {
                        //ChangesMade++; // later 
                        
                        remove = true; 
                        current = null; 
                    }
                }
                break; 

                // @* I don't RemoveEmptyArrays here anymore, since this is a structural one, 
                // and CleanJson is more for primitive types (JsonValues) *@

                case CleanAction.RemoveZeroValues: 
                Console.WriteLine("Removing Zero Values"); 
                // TO DO later
                break; 

                default: 
                Console.WriteLine("No valid CleanAction."); 
                break; 
            } 
        }
        
        return new TraverseResult{ Remove = remove, Value = current }; 
    }


    // -------------------------------------------------
    // custom return type: Traverse result: 
    // bool: remove yes /no 
    // JsonNode ... the json that got expected. 
    // -------------------------------------------------
    public TraverseResult? TraverseTree(JsonNode? json, List<CleanAction>? cleanActions) {
        // @* if (json == null) return null;  *@
        Console.WriteLine("------- Traverse Tree Start --------"); 

        if (json is JsonObject jsonObject) {
            JsonObject newobject = new JsonObject(); 
            foreach (var item in jsonObject){
                TraverseResult? result = TraverseTree(item.Value, cleanActions); 
                if (result!.Remove) { 
                    ChangesMade++; 
                    continue; 
                }
                // for null values: 
                if (result.Value == null) newobject[item.Key] = null; 
                // for none null values: 
                else newobject[item.Key] = result!.Value!.DeepClone(); // node already has a parent, might be a problem here
            }
            Console.WriteLine("returning jsonArray + ", JsonSerializer.Serialize(newobject) );  // TODO: error 
            return new TraverseResult
            {
                Remove = false, Value = newobject
            };
        }
        if (json is JsonArray jsonArray) {
            Console.WriteLine("jsonArray reached");  
            JsonArray newArray = new JsonArray(); 
            
            foreach (var item in jsonArray){
                TraverseResult? result = TraverseTree(item, cleanActions); 
                if (result!.Remove) {
                    ChangesMade++; 
                    continue; 
                } 

                newArray.Add(result.Value?.DeepClone()); // cleanedResult.Value can be null 
                // ? null conditional operator ~ similar to JS optional chaining  
                // JS = undefined but C# = null
            }
            Console.WriteLine("returning jsonArray + ", JsonSerializer.Serialize(newArray) );
            return new TraverseResult
            {
                Remove = false, Value = newArray
            }; 
        }
        if (json is JsonValue jsonValue) {
            Console.WriteLine("jsonValue reached."); 
            TraverseResult? cleanedResult = CleanJson(jsonValue, cleanActions); 
            if (cleanedResult == null) 
            {  
                Console.WriteLine("Error! Null cleanedResult == null"); 
            }  
            return cleanedResult; 
        }

        return new TraverseResult
        {
            Remove = false, Value = null // test if i initially wanted Remove = true here 
        }; 
    }


    
    // @* RemoveEmptyKeys 
    // takes a JsonElement and creates a new element without null values. 
    // returns a new JsonElement with the newly assembled data. *@
    // // C# example: if (json is JsonArray jsonArray)
    // // = pattern matching with type checking and variable declaration
    // //Check the type of node (is it a JsonArray?).
    // //Declare a new variable (jsonArray) of that type, casted from json.
    // @* 
    // examins objects, arrays and primitive values 
    
    // Parent calls recursion on child, child returns value or null: 
    // Now the parent decides:
    // child returned data → keep it
    // child returned null → omit it 
    // ...
    // traversing old structure
    // building a new cleaned structure
    // selectively copying survivors *@
    public JsonNode? RemoveEmptyKeys(JsonNode? json) {
        // examin objects, arrays and primitive values
        if (json == null) {
            return null; // don't continue if it's null 
            // this does not count for recursion (no ommitted line)
        } 
        // objects 
        if (json is JsonObject jsonObject) {
            JsonObject newObject = new JsonObject();
            foreach (var item in jsonObject){
                // copy the values if they are not null
                // Parente: I chose not to copy this property.
                JsonNode? result = RemoveEmptyKeys(item.Value); 
                if  (result != null) {   
                    newObject[item.Key] = result;
                } else {
                    // when no copy is made, ChangesMade++ 
                    ChangesMade++;  
                }
            }
            Console.WriteLine("data = " + JsonSerializer.Serialize(newObject)); 
            return newObject; 
        }
        // arrays 
        if (json is JsonArray jsonArray) {
            JsonArray newArray = new JsonArray(); 
            foreach(var item in jsonArray) {
                JsonNode? result = RemoveEmptyKeys(item); 
                if (result != null) {
                        // add entry 
                        newArray.Add(result); 
                    
                } else { // when no copy is made, ChangesMade++ 
                    ChangesMade++;   
                }
            }
        return newArray;  
        }
        // primitives
        if (json is JsonValue jsonValue) {

            if (jsonValue.GetValueKind() == JsonValueKind.Null){
                return null; 
            }
            // clone : otherwise error - ThrowInvalidOperationException_NodeAlreadyHasParent() 
            // = parent = json (the original file)
           return jsonValue.DeepClone(); 
            
        }
        return null; 
    } 


    // @* Fill missing keys in arrays of objects
    //     using union of sibling keys *@
    public JsonNode? FillMissingKeys(JsonNode json){
        // @* Console.WriteLine("inside FillMissingKeys:");  *@
        // collect all keys from sibling objects
        HashSet<string> keys = new HashSet<string>{}; 

        if (json is JsonArray jsonArray) {
            // @* Console.WriteLine("json is JsonArray");  *@
            // if (jsonArray == null) return jsonArray; 
            JsonArray newArray = new JsonArray(); 

            foreach(var arrayItem in jsonArray){
                if (arrayItem is JsonObject jsonObject){
                    // @* Console.WriteLine("arrayItem is JsonObject");  *@
                    foreach (var objectItem in jsonObject) {
                        keys.Add(objectItem.Key); 
                    }
                }
            }
        Console.WriteLine(); 
        if (keys.Count == 0) {
            Console.WriteLine("no keys collected. stopping exectuion of FillMissingKeys");
            return json;  
        } 
        foreach (var key in keys) {
            Console.WriteLine("hashSet key: " + key); 
        }
        Console.WriteLine(); 

        // rebuild every object ensuring every key exists 
        // arrayItem = the object 
        foreach (var arrayItem in jsonArray) {
            // is it an array of objects? it could be null, so: 
            if (arrayItem is not JsonObject) {
               ErrorMessage = "Invalid input. Please use an array of objects!"; 
                return null; 
            }
                // else, copy to a new object to add to jsonArray later: 
                JsonObject newObject = new JsonObject(); 

                // for each known key: 
                foreach (var key in keys){
                    // does the object contain the key? 
                    if (arrayItem is JsonObject obj && obj.ContainsKey(key)){
                        // object contains key 
                        if (arrayItem.GetValueKind() == JsonValueKind.Null) { // object could be null 
                            Console.WriteLine("null value insie the object"); 
                            newObject[key] = null; // otherwise, i would DeepClone null later
                        } else if (obj[key] == null) { // value could be null 
                            newObject[key] = null; // otherwise, i might still DeepClone null later (?)
                        } else {
                            newObject[key] = obj[key]!.DeepClone(); // arrayItem is the original object
                        }

                    } else {
                        newObject[key] = null; 
                        ChangesMade++; 
                    }
                }
                newArray.Add(newObject); 
            }

            return newArray;  
        }
        return json; 
    }

}