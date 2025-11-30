# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |

## Reporting a Vulnerability

**Please do NOT report security vulnerabilities through public GitHub issues.**

Instead, please report them via:
- **GitHub Security Advisories**: https://github.com/srenner06/AuthSmith/security/advisories
- **GitHub Issues** (with "security" label, if you're comfortable with it being public)

### What to Expect

This is a personal open-source project maintained in my spare time. While I take security seriously, please understand:

- **Response time**: I'll do my best to respond, but there's no guaranteed timeframe. It could be days, weeks, or longer depending on my availability.
- **Fixes**: Critical security issues will be prioritized, but again, timelines depend on my availability.
- **Project status**: This project may be maintained sporadically or even abandoned in the future. Use at your own risk.
- **No guarantees**: This software is provided "as-is" without warranty of any kind (see LICENSE).

**If you need guaranteed security support, consider:**
- Forking the project and maintaining it yourself
- Using a commercial authentication service
- Hiring security consultants

### What to Include in Reports

If you do find a vulnerability, please include:

- Type of issue (e.g., SQL injection, XSS, authentication bypass, etc.)
- Full paths of affected source files
- Step-by-step instructions to reproduce
- Proof-of-concept or exploit code (if possible)
- Impact assessment

## Security Best Practices for Users

### ⚠️ Important Disclaimer

**This is a personal project, not production-grade software.** While it follows security best practices, it hasn't been professionally audited. Use in production at your own risk.

### Production Deployment Recommendations

If you do choose to use this in production:

1. **Conduct your own security audit** - Review the code yourself or hire professionals
2. **Use HTTPS** - Always deploy behind HTTPS
3. **Secure your secrets**:
   - Never commit credentials to version control
   - Use environment variables or secret managers
   - Rotate keys regularly

4. **Database Security**:
   - Strong passwords
   - Limited user permissions
   - SSL/TLS connections
   - Regular encrypted backups

5. **Keep dependencies updated** - Run `dotnet outdated` regularly
6. **Monitor logs** - Set up logging and alerting
7. **Test thoroughly** - Write tests for your specific use case

### Known Security Features

- ✅ Argon2id password hashing (OWASP recommended)
- ✅ JWT with asymmetric keys (RSA/ECDSA)
- ✅ Account lockout protection
- ✅ Rate limiting
- ✅ Email enumeration protection
- ✅ Security headers (CSP, HSTS, X-Frame-Options, etc.)
- ✅ Audit logging
- ✅ CORS configuration
- ✅ Session management

### Configuration Security

1. **JWT Keys**:
   ```bash
   # Generate RSA 2048-bit keys (minimum)
   openssl genpkey -algorithm RSA -out jwt_private_key.pem -pkeyopt rsa_keygen_bits:2048
   openssl rsa -pubout -in jwt_private_key.pem -out jwt_public_key.pem
   
   # Set strict permissions
   chmod 600 jwt_private_key.pem
   ```

2. **API Keys**:
   - Use cryptographically secure random strings (32+ characters)
   - Rotate regularly
   - Different keys per environment

3. **Password Requirements**:
   - Minimum 8 characters (enforced)
   - Consider adding complexity requirements for your use case

## Compliance

Features included for common compliance needs:

- **GDPR**: Account deletion, audit trails
- **Data Minimization**: Only essential data collected
- **Audit Logs**: Security event tracking

**Note**: I'm not a lawyer. Consult legal counsel for actual compliance requirements.

## Known Limitations

As a personal project, there are limitations:

- ❌ Not professionally security audited
- ❌ No guaranteed support or response times
- ❌ May contain undiscovered vulnerabilities
- ❌ Project may be abandoned without notice
- ⚠️ Single maintainer (me) with limited time

## Recommendations

**For personal/hobby projects**: This should be fine with proper configuration.

**For small self-hosted apps**: Acceptable risk if you understand the limitations.

**For production/commercial use**: Consider:
- Professional security audit
- Commercial alternatives (Auth0, Okta, Azure AD B2C)
- Hiring a security team
- Forking and maintaining yourself

## Security Roadmap (If I Have Time)

Potential future enhancements (no promises):

- [ ] OAuth 2.0 / OpenID Connect provider support
- [ ] TOTP/Authenticator app 2FA
- [ ] WebAuthn (hardware security keys)
- [ ] Professional security audit (if funding available)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

This means:
- ✅ Free to use, modify, and distribute
- ✅ Commercial use allowed
- ❌ No warranty or support guarantees
- ❌ No liability for issues or damages
- ❌ No obligation for maintenance or updates

Use at your own risk!

## Disclaimer

**THIS SOFTWARE IS PROVIDED "AS-IS" WITHOUT WARRANTY OF ANY KIND.**

Use this software at your own risk. I make no guarantees about:
- Security
- Availability
- Support
- Maintenance
- Fitness for any particular purpose

See the LICENSE file for full legal terms.

---

**Last updated**: 2025-01-19  
**Project Status**: Active (but sporadic maintenance)  
**Maintainer**: Personal project by @srenner06
