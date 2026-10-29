using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// نظام الأصدقاء والتواصل الاجتماعي (Social & Friends Manager)
/// يدير قائمة الأصدقاء، طلبات الصداقة المعلقة، حالات الاتصال (متصل، في معركة، فاتح غرفة)،
/// والانضمام الفوري لغرف الأصدقاء بضغطة زر واحدة.
/// </summary>
public class FriendsManager : MonoBehaviour
{
    public static FriendsManager Instance { get; private set; }

    public event Action OnFriendsChanged;

    private const string PREF_FRIENDS_DATA = "BW_FriendsList_Data";
    private const string PREF_REQUESTS_DATA = "BW_FriendRequests_Data";

    [Serializable]
    public class FriendEntry
    {
        public string playerName;
        public int tierIndex;
        public int score;
        public string status; // "Online", "InMatch", "HostingRoom"
        public string hostingRoomCode;
        public string hostingRoomIP;
        public bool isOpenToFriends;
        public string lastActive;
    }

    [Serializable]
    private class FriendListWrapper
    {
        public List<FriendEntry> list = new List<FriendEntry>();
    }

    [Serializable]
    private class StringListWrapper
    {
        public List<string> list = new List<string>();
    }

    private List<FriendEntry> friends = new List<FriendEntry>();
    private List<string> pendingRequests = new List<string>();

    // حالة الغرفة المحلية التي يستضيفها اللاعب الحالي
    public static string LocalHostedRoomCode { get; private set; } = "";
    public static string LocalHostedRoomIP { get; private set; } = "127.0.0.1";
    public static bool IsLocalRoomOpenToFriends { get; private set; } = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("FriendsManager");
            obj.AddComponent<FriendsManager>();
            DontDestroyOnLoad(obj);
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadFriendsData();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void LoadFriendsData()
    {
        string friendsJson = PlayerPrefs.GetString(PREF_FRIENDS_DATA, "");
        if (!string.IsNullOrEmpty(friendsJson))
        {
            try
            {
                FriendListWrapper wrapper = JsonUtility.FromJson<FriendListWrapper>(friendsJson);
                if (wrapper != null && wrapper.list != null)
                {
                    friends = wrapper.list;
                }
            }
            catch (Exception)
            {
                friends = new List<FriendEntry>();
            }
        }

        string reqsJson = PlayerPrefs.GetString(PREF_REQUESTS_DATA, "");
        if (!string.IsNullOrEmpty(reqsJson))
        {
            try
            {
                StringListWrapper wrapper = JsonUtility.FromJson<StringListWrapper>(reqsJson);
                if (wrapper != null && wrapper.list != null)
                {
                    pendingRequests = wrapper.list;
                }
            }
            catch (Exception)
            {
                pendingRequests = new List<string>();
            }
        }

        // إذا كانت القائمة فارغة في أول تشغيل، نُنشئ مجتمعاً أولياً نابضاً بالحياة من أبطال الغابة
        if (friends.Count == 0 && pendingRequests.Count == 0)
        {
            PopulateInitialCommunity();
        }
    }

    void SaveFriendsData()
    {
        FriendListWrapper fWrapper = new FriendListWrapper { list = friends };
        PlayerPrefs.SetString(PREF_FRIENDS_DATA, JsonUtility.ToJson(fWrapper));

        StringListWrapper rWrapper = new StringListWrapper { list = pendingRequests };
        PlayerPrefs.SetString(PREF_REQUESTS_DATA, JsonUtility.ToJson(rWrapper));

        PlayerPrefs.Save();
        OnFriendsChanged?.Invoke();
    }

    void PopulateInitialCommunity()
    {
        friends.Add(new FriendEntry
        {
            playerName = "Aria Swiftwind",
            tierIndex = 5,
            score = 4250,
            status = "HostingRoom",
            hostingRoomCode = "ROOM-4821",
            hostingRoomIP = "127.0.0.1",
            isOpenToFriends = true,
            lastActive = "Now"
        });

        friends.Add(new FriendEntry
        {
            playerName = "Garrick Ironwood",
            tierIndex = 6,
            score = 6120,
            status = "InMatch",
            hostingRoomCode = "",
            hostingRoomIP = "",
            isOpenToFriends = false,
            lastActive = "5m ago"
        });

        friends.Add(new FriendEntry
        {
            playerName = "Elora Sunstrider",
            tierIndex = 7,
            score = 8400,
            status = "Online",
            hostingRoomCode = "",
            hostingRoomIP = "",
            isOpenToFriends = false,
            lastActive = "12m ago"
        });

        pendingRequests.Add("Rowan the Brave");
        pendingRequests.Add("Sylvia Oakwhisper");

        SaveFriendsData();
    }

    public List<FriendEntry> GetFriends() => new List<FriendEntry>(friends);
    public List<string> GetPendingRequests() => new List<string>(pendingRequests);

    public bool IsFriend(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return friends.Exists(f => f.playerName.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool HasPendingRequest(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        return pendingRequests.Exists(r => r.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public bool SendFriendRequest(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName)) return false;
        targetName = targetName.Trim();

        string myName = PlayerProfile.Instance != null ? PlayerProfile.Instance.PlayerName : "Player";
        if (targetName.Equals(myName, StringComparison.OrdinalIgnoreCase))
        {
            PlayerProgressionAndShopUI.Instance?.ShowNotification("⚠️ Cannot add yourself as a friend!");
            return false;
        }

        if (IsFriend(targetName))
        {
            PlayerProgressionAndShopUI.Instance?.ShowNotification($"✓ {targetName} is already on your friends list!");
            return false;
        }

        // إضافة محاكاة واقعية: إذا أرسل اللاعب طلباً لبطل في لوحة المتصدرين
        PlayerProgressionAndShopUI.Instance?.ShowNotification($"✉️ Friend request sent to {targetName}!");
        return true;
    }

    public void AcceptFriendRequest(string requesterName)
    {
        if (string.IsNullOrWhiteSpace(requesterName)) return;
        requesterName = requesterName.Trim();

        pendingRequests.RemoveAll(r => r.Equals(requesterName, StringComparison.OrdinalIgnoreCase));

        if (!IsFriend(requesterName))
        {
            int randScore = UnityEngine.Random.Range(1800, 7500);
            var tier = PlayerProfile.GetRankTier(randScore);

            friends.Add(new FriendEntry
            {
                playerName = requesterName,
                tierIndex = tier.tierIndex,
                score = randScore,
                status = "Online",
                hostingRoomCode = "",
                hostingRoomIP = "",
                isOpenToFriends = false,
                lastActive = "Just now"
            });
        }

        SaveFriendsData();
        PlayerProgressionAndShopUI.Instance?.ShowNotification($"🎉 You and {requesterName} are now friends!");
    }

    public void DeclineFriendRequest(string requesterName)
    {
        if (string.IsNullOrWhiteSpace(requesterName)) return;
        pendingRequests.RemoveAll(r => r.Equals(requesterName.Trim(), StringComparison.OrdinalIgnoreCase));
        SaveFriendsData();
        PlayerProgressionAndShopUI.Instance?.ShowNotification($"❌ Declined friend request from {requesterName}.");
    }

    public void RemoveFriend(string friendName)
    {
        if (string.IsNullOrWhiteSpace(friendName)) return;
        friends.RemoveAll(f => f.playerName.Equals(friendName.Trim(), StringComparison.OrdinalIgnoreCase));
        SaveFriendsData();
        PlayerProgressionAndShopUI.Instance?.ShowNotification($"Removed {friendName} from friends.");
    }

    // ==========================================
    // إدارة الغرف المفتوحة للأصدقاء (Room Presence)
    // ==========================================
    public static void SetLocalPlayerRoomHosting(string roomCode, string ip, bool openToFriends)
    {
        LocalHostedRoomCode = roomCode;
        LocalHostedRoomIP = string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;
        IsLocalRoomOpenToFriends = openToFriends;
    }

    public static void ClearLocalPlayerRoomHosting()
    {
        LocalHostedRoomCode = "";
        IsLocalRoomOpenToFriends = false;
    }
}
