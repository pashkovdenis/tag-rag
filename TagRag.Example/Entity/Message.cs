namespace TagRag.Example.Entity
{
    public class Message
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string User { get; set; } = "";
        public string Content { get; set; } = "";
    }

}
