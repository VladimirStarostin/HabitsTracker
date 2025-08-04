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

    module Security =

        type IPasswordHasher =
            abstract Hash   : plain:string -> Result<string, string>
            abstract Verify : suppliedPlain:string * storedHash:string -> Result<unit, string>

        module private Internal =
            let constantTimeEquals (a: byte[]) (b: byte[]) =
                if isNull a || isNull b || a.Length <> b.Length then false
                else
                    let mutable diff = 0
                    for i = 0 to a.Length - 1 do
                        diff <- diff ||| int (a[i] ^^^ b[i])
                    diff = 0

        open System.Security.Cryptography

        // pbkdf2-sha256$<iter>$<salt_b64>$<hash_b64>
        type Pbkdf2PasswordHasher(iterations : int, saltSize : int, hashSize : int) =
            let hashInternal (plain:string) =
                if String.IsNullOrWhiteSpace plain then
                    Error "Password is empty."
                else
                    try
                        let salt = Array.zeroCreate<byte> saltSize
                        RandomNumberGenerator.Fill(salt)
                        use kdf = new Rfc2898DeriveBytes(plain, salt, iterations, HashAlgorithmName.SHA256)
                        let hash = kdf.GetBytes hashSize
                        let packed =
                            String.concat "$" [
                                "pbkdf2-sha256"
                                string iterations
                                Convert.ToBase64String salt
                                Convert.ToBase64String hash
                            ]
                        Ok packed
                    with ex ->
                        Error ex.Message

            let verifyInternal (plain:string) (stored:string) =
                if String.IsNullOrWhiteSpace plain || String.IsNullOrWhiteSpace stored then
                    Error "Invalid input."
                else
                    try
                        let parts = stored.Split('$')
                        if parts.Length = 4 && parts[0] = "pbkdf2-sha256" then
                            let iters = int parts[1]
                            let salt  = Convert.FromBase64String parts[2]
                            let want  = Convert.FromBase64String parts[3]
                            use kdf = new Rfc2898DeriveBytes(plain, salt, iters, HashAlgorithmName.SHA256)
                            let got = kdf.GetBytes(want.Length)
                            if Internal.constantTimeEquals want got then Ok ()
                            else Error "Invalid password."
                        else
                            Error "Unsupported password hash format."
                    with ex ->
                        Error ex.Message

            interface IPasswordHasher with
                member _.Hash plain = hashInternal plain
                member _.Verify (supplied, stored) = verifyInternal supplied stored

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
