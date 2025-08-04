module UserService.Host.Domain.Common

open System

module Types =
    type AuthResponse =
        { AccessToken: string
          RefreshToken: string }

    type JwtSettings =
        { Secret: string
          Issuer: string
          Audience: string
          AccessTokenValidityMinutes: int
          RefreshTokenValidityDays: int }

    type RefreshToken =
        { UserId: int
          Token: string
          ExpiresAt: DateTime
          IsRevoked: bool }

module Operations =

    open System.Data
    open System.Security.Claims
    open System.IdentityModel.Tokens.Jwt
    open System.Text
    open System.Threading.Tasks

    open Microsoft.IdentityModel.Tokens

    open HabitsTracker.Helpers.Pipelines

    open Types
    open UserService.Host.DataAccess.RefreshTokens

    let createRefreshToken<'err> (userId: int) (jwtSettings: JwtSettings) : PipelineResult<RefreshToken, 'err> =
        let newRefreshToken = Guid.NewGuid().ToString "N"
        let newExpiry = DateTime.UtcNow.AddDays (float jwtSettings.RefreshTokenValidityDays)

        Ok
            { Token = newRefreshToken
              UserId = userId
              ExpiresAt = newExpiry
              IsRevoked = false }

    let saveNewTokenAsync<'err> (newToken: RefreshToken) (conn: IDbConnection) : Task<PipelineResult<string, 'err>> =
        task {
            let! _ =
                insertSingleAsync
                    { Token = newToken.Token
                      UserId = newToken.UserId
                      ExpiresAt = newToken.ExpiresAt
                      IsRevoked = newToken.IsRevoked
                      CreatedAt = DateTime.UtcNow }
                    conn

            return Ok newToken.Token
        }

    let generateJwt<'err>
        (newRefreshToken: string)
        (userId: int)
        (jwtSettings: JwtSettings)
        : PipelineResult<AuthResponse, 'err> =
        let claims =
            [ Claim (JwtRegisteredClaimNames.Sub, userId.ToString ())
              Claim (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString ()) ]

        let key = SymmetricSecurityKey (Encoding.UTF8.GetBytes (jwtSettings.Secret))
        let creds = SigningCredentials (key, SecurityAlgorithms.HmacSha256)
        let now = DateTime.UtcNow
        let expires = now.AddMinutes (float jwtSettings.AccessTokenValidityMinutes)

        let jwt =
            JwtSecurityToken (
                issuer = jwtSettings.Issuer,
                audience = jwtSettings.Audience,
                claims = claims,
                notBefore = Nullable now,
                expires = Nullable expires,
                signingCredentials = creds
            )

        Ok
            { AccessToken = JwtSecurityTokenHandler().WriteToken (jwt)
              RefreshToken = newRefreshToken }
