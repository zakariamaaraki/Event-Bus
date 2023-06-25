namespace Service_bus.Models;

public enum QueueType
{
    [DisplayText("Queue")]
    Queue,

    [DisplayText("DeadLetterQueue")]
    DeadLetterQueue,

    [DisplayText("Partition")]
    Partition
}

public class DisplayText : Attribute
{

    public DisplayText(string Text)
    {
        this.text = Text;
    }

    private string text;


    public string Text
    {
        get { return text; }
        set { text = value; }
    }
}
