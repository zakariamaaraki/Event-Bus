using System.Text;

namespace Service_bus.Models;

public class Event : AbstractEvent
{
    public byte[]? Key { get; set; }

    public byte[]? Body { get; set; }

    public override string Serialize()
    {
        string base64HeaderContent = TransformHeaderValuesToBase64();

        return Convert.ToBase64String(Key ?? new byte[0])
                + "," + base64HeaderContent
                + "," + Convert.ToBase64String(Body ?? new byte[0]);
    }

    private string TransformHeaderValuesToBase64()
    {
        StringBuilder headerContent = new();

        foreach ((string key, string value) in Header)
        {
            headerContent.Append(Convert.ToBase64String(EncodeStringToBytes(key)));
            headerContent.Append(";");
            headerContent.Append(Convert.ToBase64String(EncodeStringToBytes(value)));
            headerContent.Append(";;");
        }

        return headerContent.ToString();
    }

    private byte[] EncodeStringToBytes(string content)
    {
        return System.Text.UnicodeEncoding.Default.GetBytes(content);
    }

    private string DecodeBase64ToString(byte[] byteArray)
    {
        return System.Text.UnicodeEncoding.Default.GetString(byteArray);
    }

    private Dictionary<string, string> BuildHeaderValuesFromBase64String(string base64HeaderContent)
    {
        string[] headers = base64HeaderContent.Split(";;");
        foreach (string[] header in headers
                                        .Where(header => header.Length > 1)
                                        .Select(header => header.Split(";")))
        {
            byte[] key = Convert.FromBase64String(header[0]);
            byte[] value = Convert.FromBase64String(header[1]);
            Header[DecodeBase64ToString(key)] = DecodeBase64ToString(value);
        }
        return Header;
    }

    public override Event Deserialize(string stringEvent)
    {
        string[] content = stringEvent.Split(",");
        Key = Convert.FromBase64String(content[0]);
        BuildHeaderValuesFromBase64String(content[1]);
        Body = Convert.FromBase64String(content[2]);
        return this;
    }
}