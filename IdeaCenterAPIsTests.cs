using QA_IdeaCenterTest.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Text.Json;

namespace QA_IdeaCenterTest;

[TestFixture]

public class IdeaCenterAPIsTests
{
    private RestSharp.RestClient client;
    private string lastIdeaId = string.Empty;

    private string JwtToken = null;

    private string Email = Environment.GetEnvironmentVariable("IDEA_CENTER_EMAIL");
    private string Password = Environment.GetEnvironmentVariable("IDEA_CENTER_PASS");


    private string BaseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

    private string GetJwtToken(string email, string password)
    {
        var tmpClient = new RestSharp.RestClient(BaseUrl);
        var request = new RestSharp.RestRequest("/api/User/Authentication", Method.Post);
        request.AddJsonBody(new { email, password });
        var response = tmpClient.Execute(request);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var token = content.GetProperty("accessToken").GetString();
            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }
        throw new Exception($"Failed to get JWT token: {response.Content}");
    }

    [SetUp]
    public void Setup()
    {
        if (string.IsNullOrWhiteSpace(JwtToken))
            JwtToken = GetJwtToken(Email, Password);

        var options = new RestSharp.RestClientOptions(BaseUrl)
        {
            Authenticator = new JwtAuthenticator(JwtToken),
        };
        this.client = new RestSharp.RestClient(options);
    }

    [Order(1)]
    [Test]
    public void CreateIdea_WithRequiredFeilds_ShouldReturnSucces()
    {
        // Arrange
        var request = new RestSharp.RestRequest("/api/Idea/Create", Method.Post);
        var ideaRequest = new IdeaDTO
        {
            Title = "Test Idea",
            Description = "This is a test idea",
            Url = ""
        };
        request.AddJsonBody(ideaRequest);

        // Act
        var response = this.client.Execute(request);
        var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));       
        Assert.That(content, Is.Not.Null);
        Assert.That(content.Message, Is.EqualTo("Successfully created!"));
    }

    [Order(2)]
    [Test]
    public void GetAllIdeas_ShouldReturnList()
    {
        // Arrange
        var request = new RestSharp.RestRequest($"/api/Idea/All", Method.Get);
        
        // Act
        var response = this.client.Execute(request);
        var content = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
        
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(content, Is.Not.Null);
        Assert.That(content.Count, Is.GreaterThan(0), "Expected at least one idea to be returned.");
        lastIdeaId = content.LastOrDefault()?.Id ?? string.Empty;
    }

    [Order(3)]
    [Test]
    public void EditLastCreatedIdea_ShouldReturnSuccess()
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(lastIdeaId))
        {
            var tmpRequest = new RestSharp.RestRequest($"/api/Idea/All", Method.Get);
            var tmpResponse = this.client.Execute(tmpRequest);
            var tmpContent = JsonSerializer.Deserialize<List<ApiResponseDTO>>(tmpResponse.Content);
            Assert.That(tmpContent.Count, Is.GreaterThan(0), "Expected to have at least allready created.");
            lastIdeaId = tmpContent.LastOrDefault()?.Id ?? string.Empty;
        }

        var ideaRequest = new IdeaDTO
        {
            Title = "Updated Test Idea",
            Description = "This is an updated test idea",
            Url = ""
        };

        var request = new RestSharp.RestRequest($"/api/Idea/Edit", Method.Put);
        request.AddQueryParameter("ideaId", lastIdeaId);
        request.AddJsonBody(ideaRequest);

        // Act
        var response = this.client.Execute(request);

        // Assert
        var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(content, Is.Not.Null);
        Assert.That(content.Message, Is.EqualTo("Edited successfully"));
    }

    [Order(4)]
    [Test]
    public void DeleteLastCreatedIdea_ShouldReturnSuccess()
    {
        // Arrange
        if (string.IsNullOrWhiteSpace(lastIdeaId))
        {
            var tmpRequest = new RestSharp.RestRequest($"/api/Idea/All", Method.Get);
            var tmpResponse = this.client.Execute(tmpRequest);
            var tmpContent = JsonSerializer.Deserialize<List<ApiResponseDTO>>(tmpResponse.Content);
            Assert.That(tmpContent.Count, Is.GreaterThan(0), "Expected to have at least allready created.");
            lastIdeaId = tmpContent.LastOrDefault()?.Id ?? string.Empty;
        }
        var request = new RestSharp.RestRequest($"/api/Idea/Delete", Method.Delete);
        request.AddQueryParameter("ideaId", lastIdeaId);

        // Act
        var response = this.client.Execute(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
        Assert.That(response.Content, Does.Contain("The idea is deleted!"));
    }

    [Order(5)]
    [Test]
    public void CreatingIdeaWithoutRequaredField_SchouldReturn_BadRequest()
    {
        // Arrange
        var request = new RestSharp.RestRequest("/api/Idea/Create", Method.Post);
        var ideaRequest = new IdeaDTO
        {
            Title = "",
            Description = ""
        };
        request.AddJsonBody(ideaRequest);

        // Act
        var response = this.client.Execute(request);
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
    }

    [Order(6)]
    [Test]
    public void TryToEditNonExistingIdea_ShouldReturnNotFound()
    {
        // Arrange
        var request = new RestSharp.RestRequest($"/api/Idea/Edit", Method.Put);
        request.AddQueryParameter("ideaId", "non-existing-id");
        var ideaRequest = new IdeaDTO
        {
            Title = "Updated Test Idea",
            Description = "This is an updated test idea",
            Url = ""
        };
        request.AddJsonBody(ideaRequest);
        // Act
        var response = this.client.Execute(request);
        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.BadRequest));
        Assert.That(response.Content, Does.Contain("There is no such idea!"));
    }

    [Order(7)]
    [Test]
    public void TryToDeleteNonExistingIdea_ShouldReturnNotFound()
    {
        // Arrange
        var request = new RestSharp.RestRequest($"/api/Idea/Delete", Method.Put);
        request.AddQueryParameter("ideaId", "non-existing-id");

        // Act
        var response = this.client.Execute(request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.MethodNotAllowed));
    }

    [TearDown]
    public void TearDown()
    {
        this.client?.Dispose();
    }
}