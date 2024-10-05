using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using STP.UserManagement.Identity.Client;
using System.Security.Claims;

public class Me
{
	private readonly ITokenProvider tokenProvider;
	private readonly IConfiguration config;

	public Me(ITokenProvider tokenProvider, IConfiguration config)
	{
		this.tokenProvider = tokenProvider;
		this.config = config;
	}

	public async Task<Guid> Id()
	{
		var accessToken = await tokenProvider.AcquireAccessToken();
		var id = GetClaims(accessToken).Where(c => c.Type == "sub").Select(c => (Guid?)Guid.Parse(c.Value)).SingleOrDefault();
		return id ?? throw new ArgumentNullException();
	}

	public async Task<Guid> TenantId()
	{
		var accessToken = await tokenProvider.AcquireAccessToken();
		var tenantId = GetClaims(accessToken).Where(c => c.Type == "tid").Select(c => (Guid?)Guid.Parse(c.Value)).SingleOrDefault()
			?? GetClaims(accessToken).Where(c => c.Type == "client_tid").Select(c => (Guid?)Guid.Parse(c.Value)).SingleOrDefault();

		return tenantId ?? throw new ArgumentNullException();
	}

	private IEnumerable<Claim> GetClaims(Token token)
	{
		var accessToken = token.AccessToken;
		var jwtReader = new JsonWebTokenHandler();
		var jwt = jwtReader.ReadJsonWebToken(accessToken);
		return jwt.Claims;
	}
}