using Photon.Realtime;

namespace Utils
{
    public static class PhotonRoomExtensions
    {
        public static void SetCustomProperty(this Room room, string propName, object value)
        {
            ExitGames.Client.Photon.Hashtable prop = new ExitGames.Client.Photon.Hashtable();
            prop.Add(propName, value);
            room.SetCustomProperties(prop);
        }
 
        public static void SetCustomProperty(this Room room, string propName, object value, object oldValue)
        {
            ExitGames.Client.Photon.Hashtable prop = new ExitGames.Client.Photon.Hashtable();
            prop.Add(propName, value);
            ExitGames.Client.Photon.Hashtable oldvalueProp = new ExitGames.Client.Photon.Hashtable();
            oldvalueProp.Add(propName, oldValue);
            room.SetCustomProperties(prop, oldvalueProp);
        }
    }
}
