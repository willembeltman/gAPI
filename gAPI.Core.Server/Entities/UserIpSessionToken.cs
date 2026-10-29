using gAPI.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gAPI.Core.Server.Entities;

[IsHidden]
public class UserIpSessionToken
{
    public UserIpSessionToken() { }
    public UserIpSessionToken(
        UserIpSession userIpSessionCompany,
        UserToken? token)
    {
        UserIpSessionId = userIpSessionCompany.Id;
        TokenId = token?.Id;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    public long? TokenId { get; set; }
    public virtual UserToken? Token { get; set; }

    public long UserIpSessionId { get; set; }
    public virtual UserIpSession? UserIpSession { get; set; }

    public virtual ICollection<UserIpSessionTokenRoute>? UserIpSessionTokenRoutes { get; set; }
}