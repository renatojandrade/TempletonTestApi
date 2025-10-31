using System.Runtime.Serialization;

namespace TempletonTestApi.Clients.Enums;

public enum ItemType
{
    Unknown = 0,

    [EnumMember(Value = "story")]
    Story = 1,

    [EnumMember(Value = "comment")]
    Comment = 2,

    [EnumMember(Value = "job")]
    Job = 3,

    [EnumMember(Value = "poll")]
    Poll = 4,

    [EnumMember(Value = "pollopt")]
    PollOption = 5
}
