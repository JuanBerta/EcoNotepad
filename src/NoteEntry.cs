using System;

namespace EcoNotepad
{
    public class NoteEntry
    {
        public string Id { get; set; }
        public string Item { get; set; }
        public string Price { get; set; }
        public string Store { get; set; }
        public string Qty { get; set; }
        public string Tags { get; set; }
        public string Notes { get; set; }
        public bool Done { get; set; }

        public NoteEntry()
        {
            Id = Guid.NewGuid().ToString("N");
            Item = "";
            Price = "";
            Store = "";
            Qty = "";
            Tags = "";
            Notes = "";
            Done = false;
        }
    }
}
