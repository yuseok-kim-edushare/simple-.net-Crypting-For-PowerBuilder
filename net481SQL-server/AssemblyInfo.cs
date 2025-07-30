using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

// SQL CLR security settings
// AllowPartiallyTrustedCallers is required for SQL CLR functionality
[assembly: AllowPartiallyTrustedCallers]
// Level2 provides better performance and is required for SQL CLR and cryptographic operations
[assembly: SecurityRules(SecurityRuleSet.Level2)] 