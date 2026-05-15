namespace CleanJsonApp.Models; 

public class SavedJson {
    public string Json { get; set; } = ""; 
    public DateTime CreatedAt { get; set; } 
    public int Changes { get; set; }  
}