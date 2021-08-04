namespace Delivery_Link.Types
{
    public class IncommingMessage
    {
        private string _timeRecieved;
        private string _type;
        private string _callsign;
        private string _content;

        public string timeRecieved { get { return _timeRecieved; } set { _timeRecieved = value; } }
        public string type { get { return _type; } set { _type = value; } }
        public string callsign { get { return _callsign; } set { _callsign = value; } }
        public string content { get { return _content; } set { _content = value; } }

    }

}
