namespace Parser;

public class CaptchaVerifier
{
    public CaptchaVerifier(string token = "null", bool ok = false)
    {
        this.ok = ok;
        this.token = token;
    }

    public bool ok { get; set; }
    public string token { get; set; }
}