namespace ClassLibrary;

public record RecieveClientCommandData
{
    public RecieveClientCommandData(string key, string value)
    {
        Key = key; 
        Value = value;
    }
    string? Key { get; set; }
    string? Value { get; set; }
}
