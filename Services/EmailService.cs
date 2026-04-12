using System.Net;
using System.Net.Mail;

namespace CyberSecurityWebApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendConfirmationEmailAsync(string toEmail, string toName, string confirmationLink)
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:Username"] ?? "";
            var smtpPass = _config["Email:Password"] ?? "";
            var fromEmail = _config["Email:From"] ?? smtpUser;
            var fromName = _config["Email:FromName"] ?? "Cyber Security Akademija";

            var subject = "✅ Potrdite vaš e-poštni naslov – Cyber Security Akademija";

            var body = $@"
<!DOCTYPE html>
<html lang='sl'>
<head><meta charset='utf-8'/></head>
<body style='margin:0;padding:0;background:#f4f6f9;font-family:Segoe UI,Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f4f6f9;padding:40px 20px;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0' style='background:#fff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>

        <!-- Header -->
        <tr>
          <td style='background:linear-gradient(135deg,#1a1a2e,#16213e);padding:36px 40px;text-align:center;'>
            <div style='font-size:2.2rem;margin-bottom:10px;'>🛡️</div>
            <h1 style='color:#fff;font-size:1.4rem;font-weight:800;margin:0;letter-spacing:-0.02em;'>
              Cyber Security Akademija
            </h1>
            <p style='color:rgba(255,255,255,0.6);font-size:0.85rem;margin:6px 0 0;'>
              Potrditev e-poštnega naslova
            </p>
          </td>
        </tr>

        <!-- Body -->
        <tr>
          <td style='padding:36px 40px;'>
            <h2 style='color:#1a1a1a;font-size:1.2rem;font-weight:700;margin:0 0 12px;'>
              Pozdravljeni, {toName}! 👋
            </h2>
            <p style='color:#555;font-size:0.93rem;line-height:1.65;margin:0 0 24px;'>
              Hvala za registracijo! Za dokončanje postopka morate potrditi vaš e-poštni naslov.
              Kliknite spodnji gumb v naslednjih <strong>24 urah</strong>.
            </p>

            <!-- CTA Button -->
            <div style='text-align:center;margin:28px 0;'>
              <a href='{confirmationLink}'
                 style='display:inline-block;background:linear-gradient(135deg,#2563eb,#1d4ed8);
                        color:#fff;font-weight:700;font-size:1rem;padding:14px 36px;
                        border-radius:12px;text-decoration:none;
                        box-shadow:0 4px 16px rgba(37,99,235,0.35);'>
                ✅ Potrdi e-poštni naslov
              </a>
            </div>

            <p style='color:#888;font-size:0.82rem;line-height:1.6;margin:0 0 8px;'>
              Če gumb ne deluje, kopirajte in prilepite to povezavo v brskalnik:
            </p>
            <p style='color:#2563eb;font-size:0.78rem;word-break:break-all;margin:0 0 24px;'>
              {confirmationLink}
            </p>

            <div style='background:#fff8e1;border:1.5px solid #ffc107;border-radius:10px;
                        padding:14px 18px;font-size:0.85rem;color:#6d4c00;'>
              ⚠️ Če niste ustvarili računa, ta e-pošto varno ignorirajte.
              Povezava bo potekla v 24 urah.
            </div>
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td style='background:#f8f9fa;padding:20px 40px;border-top:1px solid #eee;
                     text-align:center;font-size:0.78rem;color:#aaa;'>
            © {DateTime.Now.Year} Cyber Security Akademija &nbsp;·&nbsp; Avtomatsko sporočilo
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message);
        }
    }
}