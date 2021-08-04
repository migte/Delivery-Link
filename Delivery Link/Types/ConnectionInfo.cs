namespace Delivery_Link.Types
{
    // Holds connection information (login code and callsign).
    public class ConnectionInfo
    {
        string _loginCode;
        string _callsign;

        public string loginCode
        {
            get { return _loginCode; }
            set { _loginCode = value; }
        }
        public string callsign
        {
            get { return _callsign; }
            set { _callsign = value; }
        }
    }
}
