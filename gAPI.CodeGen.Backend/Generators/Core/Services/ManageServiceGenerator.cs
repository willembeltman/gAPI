//using gAPI.CodeGen.Backend.Models;

//namespace gAPI.CodeGen.Backend.Generators.Core.Services;

//public class ManageServiceGenerator : BaseGenerator
//{
//    public ManageServiceGenerator(
//        BackendGenerator context)
//    {
//        Directory = context.Config.Core_ServicesDirectory;
//        Namespace = context.Config.Core_ServicesNamespace;

//        Context = context;

//        Name = "ManageService";
//        FileName = $"{Name}.cs";
//    }

//    public BackendGenerator Context { get; }

//    public SharedReference IServerAuthenticationService => Context.IServerAuthenticationService;
//    public SharedReference IServerAuthenticationSecurity => Context.IServerAuthenticationSecurity;
//    public SharedReference IManageService => Context.IManageService;
//    public SharedReference BaseResponseT => Context.SharedReferences.BaseResponseT;
//    public SharedReference ChangePasswordRequest => Context.ChangePasswordRequest;
//    public SharedReference ChangePasswordResponse => Context.ChangePasswordResponse;
//    public SharedReference StringExtensions => Context.SharedReferences.StringExtensions;

//    public void GenerateCode()
//    {
//        Reg(IServerAuthenticationService);
//        Reg(IServerAuthenticationSecurity);
//        Reg(IManageService);
//        Reg(BaseResponseT);
//        Reg(ChangePasswordRequest);
//        Reg(ChangePasswordResponse);
//        Reg(StringExtensions);

//        Code = $@"{GetNamespacesCode()}
//namespace {Namespace};

//public class {Name}(
//    {IServerAuthenticationService} serverAuthenticationService,
//    {IServerAuthenticationSecurity} serverAuthenticationSecurity) 
//    : {IManageService}
//{{
//    public async Task<{BaseResponseT}<{ChangePasswordResponse}>> ChangePassword({ChangePasswordRequest} request, CancellationToken ct)
//    {{
//        var result = await ChangePassword(
//            request.Password,
//            request.NewPassword, 
//            request.NewPasswordRepeat,
//            ct);

//        if (!result.Success)
//            return new {BaseResponseT}<{ChangePasswordResponse}>()
//            {{
//                Response = result,
//                ErrorNotAuthorized = true
//            }};

//        return new {BaseResponseT}<{ChangePasswordResponse}>()
//        {{
//            Success = true,
//            Response = result
//        }};
//    }}
//    public async Task<{ChangePasswordResponse}> ChangePassword(string password, string newPassword, string newPasswordRepeat, CancellationToken ct)
//    {{
//        if (serverAuthenticationService.State.DbUser == null)
//            return new {ChangePasswordResponse}();

//        if (newPassword != newPasswordRepeat)
//            return new {ChangePasswordResponse}()
//            {{
//                ErrorPasswordsDoNotMatch = true
//            }};

//        var allowedBefore = await serverAuthenticationSecurity.BeforeChangePasswordAsync(ct);
//        if (!allowedBefore)
//            return new {ChangePasswordResponse}()
//            {{
//                ErrorLockedOut = true
//            }};

//        var passwordHash = StringExtensions.HashString(password);
//        if (serverAuthenticationService.State.DbUser.PasswordHash != passwordHash)
//            return new {ChangePasswordResponse}()
//            {{
//                ErrorLockedOut = await serverAuthenticationSecurity.AfterUnSuccesfullChangePasswordAsync(ct)
//            }};

//        var allowedAfter = await serverAuthenticationSecurity.AfterSuccesfullChangePasswordAsync(ct);
//        if (!allowedAfter)
//            return new {ChangePasswordResponse}()
//            {{
//                ErrorLockedOut = true
//            }};

//        var newPasswordHash = {StringExtensions}.HashString(newPassword);
//        serverAuthenticationService.State.DbUser.PasswordHash = newPasswordHash;
//        await serverAuthenticationService.SaveChangesAsync(ct);

//        return new {ChangePasswordResponse}()
//        {{
//            Success = true
//        }};
//    }}
//}}";
//        Save(false);
//    }
//}