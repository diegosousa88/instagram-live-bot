<Query Kind="Program">
  <NuGetReference>Selenium.Support</NuGetReference>
  <NuGetReference Version="3.141.0">Selenium.WebDriver</NuGetReference>
  <Namespace>OpenQA.Selenium.Chrome</Namespace>
  <Namespace>OpenQA.Selenium</Namespace>
  <Namespace>OpenQA.Selenium.Remote</Namespace>
</Query>

static int MillisecondsToSeconds = 1000;
static int SecondsToMinutes = 60 * MillisecondsToSeconds;
static int DefaultInterval = 4 * MillisecondsToSeconds;

public class BrowserFactory
{
	const string ChromeDriverDirectory = @"c:\Selenium\";
	const string ChromeExecutablePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";
	
	public RemoteWebDriver Create() => new ChromeDriver(ChromeDriverDirectory, GetChromeOptions());
	
	public ChromeOptions GetChromeOptions()
	{
		var chromeOptions = new ChromeOptions();
		chromeOptions.AddArguments(new List<string>
		{
			"--test-type", "--dns-prefetch-disable", "--allow-outdated-plugins", "--always-authorize-plugins",
			"--allow-failed-policy-fetch-for-test", "--allow-insecure-localhost", "--allow-running-insecure-content",
			"--reduce-security-for-testing", "--use-test-config", "--disable-infobars", "--start-maximized",
			"--no-sandbox", "--incognito"
		});
		chromeOptions.AddUserProfilePreference("browser.helperApps.neverAsk.saveToDisk", "application/zip, application/octet-stream, text/csv");
		chromeOptions.AddUserProfilePreference("download.default_directory", null);
		chromeOptions.BinaryLocation = ChromeExecutablePath;
		chromeOptions.SetLoggingPreference(OpenQA.Selenium.LogType.Browser, OpenQA.Selenium.LogLevel.All);
		
		return chromeOptions;
	}
}

public class LoginProcessor
{
	const string xPathTemplate = "//input[@name='{0}']";
	private string _userMode;
	private RemoteWebDriver _remoteWebDriver;
	private string _baseUserName;
	private string _password;
	
	public LoginProcessor(RemoteWebDriver remoteWebDriver)
	{
		_remoteWebDriver = remoteWebDriver;
	}
	
	public void Configure(string userMode, string baseUserName, string password)
	{
		_userMode = userMode;
		_baseUserName = $"{baseUserName}.";
		_password = password;
	}
	
	public void Process()
	{
		var usernameInput = _remoteWebDriver.FindElementByXPath(string.Format(xPathTemplate, "username"));
		usernameInput.SendKeys($"{_baseUserName}{_userMode}");

		var passwordInput = _remoteWebDriver.FindElementByXPath(string.Format(xPathTemplate, "password"));
		passwordInput.SendKeys(_password);
		passwordInput.SendKeys(Keys.Enter);
		
		Thread.Sleep(DefaultInterval);
		var buttonSaveLoginInfo = _remoteWebDriver.FindElementByXPath("//button[text()='Not Now']");
		buttonSaveLoginInfo.Click();
	}
}

public class CommentSender
{	
	const string JsAddTextToInput = @"
		var elm = arguments[0], txt = arguments[1];
		elm.value += txt;
		elm.dispatchEvent(new Event('input', { bubbles: true }));";
	const int MinIntervalInMinutes = 1;
	const int MaxIntervalInMinutes = 3;
	
	string _userMode;
	RemoteWebDriver _remoteWebDriver;
	List<string> _comments;
	IWebElement _commentTextArea;
	readonly Random _selectCommentRandom;
	readonly Random _sendIntervalRandom;

	public CommentSender(RemoteWebDriver remoteWebDriver, string userMode)
	{
		_remoteWebDriver = remoteWebDriver;
		_userMode = userMode;
		
		_selectCommentRandom = new Random();
		_sendIntervalRandom = new Random();
		
		LoadComments();
	}

	public void Send()
	{
		SetCommentInput();
		
		while (true)
			SendComment(GetCommentRandomly());
	}
	
	private void SetCommentInput() =>
		_commentTextArea = _remoteWebDriver.FindElementByXPath("//textarea[@placeholder='Add a comment…']");
		
	private string GetCommentRandomly() =>
		_comments[_selectCommentRandom.Next(0, _comments.Count)];
		
	private void SendComment(string message)
	{
		PlaceComment(_commentTextArea, message);
		SubmitAndWait();
	}
	
	private void SubmitAndWait()
	{
		_commentTextArea.SendKeys(Keys.Enter);
		Thread.Sleep(ComputeSubmitInterval());
	}
	
	private int ComputeSubmitInterval() =>
		_sendIntervalRandom.Next(MinIntervalInMinutes, MaxIntervalInMinutes) * SecondsToMinutes;
	
	private void PlaceComment(IWebElement commentInput, string message)
	{
		commentInput.Click();
		_remoteWebDriver.ExecuteScript(JsAddTextToInput, commentInput, message);
		commentInput.SendKeys(" ");
	}

	private void LoadComments()
	{
		if (_comments == null)
			_comments = new List<string>();
		
		using (var fileReader = new StreamReader(Path.Combine(Util.MyQueriesFolder, $"{_userMode}-comments.txt")))
			while (!fileReader.EndOfStream)
				_comments.Add(fileReader.ReadLine());
	}
}

void Main(string[] args)
{
	// Uncomment the line below for testing in LinqPad IDE.
	// userMode: "hater" for hating comments
	// 			 "follower" for polite comments
	// liveUserName: "diegosousa88.follower" for development
	//               "diegosousa88" for production
	//args = new string[] { "hater", "diegosousa88", "[PASSWORD]" };

	string userMode = args[0];
	string liveUserName = args[1];
	string password = args[2];
		
	// Create browser instance.
	var browserFactory = new BrowserFactory();
	var webDriver = browserFactory.Create();
	
	// Navigate to Instagram login page.
	webDriver.Navigate().GoToUrl("https://www.instagram.com");
	Thread.Sleep(DefaultInterval);

	// Login user.
	var loginProcessor = new LoginProcessor(webDriver);
	loginProcessor.Configure(userMode, liveUserName, password);
	loginProcessor.Process();

	// Navigate to Instagram live page.
	webDriver.Navigate().GoToUrl($"https://www.instagram.com/{liveUserName}/live");
	//webDriver.Navigate().GoToUrl($"https://www.instagram.com/{liveUserName}.follower/live");
	
	// Send comments.
	var commentSender = new CommentSender(webDriver, userMode);
	commentSender.Send();
}