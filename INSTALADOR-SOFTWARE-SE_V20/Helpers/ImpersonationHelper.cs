using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ComponentModel;

public class ImpersonationHelper
{
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
    private const int LOGON32_PROVIDER_DEFAULT = 0;

    public static void RunAsUser(string domain, string username, string password, Action actionToRun)
    {
        IntPtr token = IntPtr.Zero;
        if (!LogonUser(username, domain, password, LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_DEFAULT, out token))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            WindowsIdentity.RunImpersonated(new Microsoft.Win32.SafeHandles.SafeAccessTokenHandle(token), () =>
            {
                // Todo o código executado aqui dentro será no contexto do usuário personificado
                actionToRun();
            });
        }
        finally
        {
            if (token != IntPtr.Zero)
            {
                CloseHandle(token);
            }
        }
    }
}