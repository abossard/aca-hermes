from locust import HttpUser, task, between
import urllib.parse


class HermesUser(HttpUser):
    """
    Load test user for the Hermes Aspire application.
    Tests both direct API calls and proxy calls.
    """
    
    # Wait between 1 and 3 seconds between tasks
    wait_time = between(1, 3)
    
    def on_start(self):
        """Called when a user starts - can be used for login etc."""
        pass
    
    @task(3)
    def test_direct_api(self):
        """Test direct API call to weather endpoint"""
        with self.client.get("/weatherforecast", catch_response=True) as response:
            if response.status_code == 200:
                response.success()
            else:
                response.failure(f"Got status code {response.status_code}")
    
    @task(2) 
    def test_proxy_external(self):
        """Test proxy with external API call"""
        external_url = "https://httpbin.org/json"
        encoded_url = urllib.parse.quote(external_url, safe='')
        
        with self.client.get(f"/proxy?url={encoded_url}", catch_response=True) as response:
            if response.status_code == 200:
                response.success()
            else:
                response.failure(f"Proxy call failed with status {response.status_code}")
    
    @task(4)
    def test_proxy_internal(self):
        """Test proxy with internal service discovery"""
        # This uses Aspire service discovery to call the API service through proxy
        internal_url = "https+http://apiservice/weatherforecast"
        encoded_url = urllib.parse.quote(internal_url, safe='')
        
        with self.client.get(f"/proxy?url={encoded_url}", catch_response=True) as response:
            if response.status_code == 200:
                response.success()
            else:
                response.failure(f"Internal proxy call failed with status {response.status_code}")
    
    @task(1)
    def test_health_check(self):
        """Test health check endpoint"""
        with self.client.get("/health", catch_response=True) as response:
            if response.status_code == 200:
                response.success()
            else:
                response.failure(f"Health check failed with status {response.status_code}")


class ApiServiceUser(HttpUser):
    """
    Specialized user for testing API service directly.
    Use this class when you want to focus load testing on the API service only.
    """
    
    wait_time = between(0.5, 2)
    
    @task
    def get_weather(self):
        """Direct weather API call"""
        self.client.get("/weatherforecast")
    
    @task
    def health_check(self):
        """API service health check"""
        self.client.get("/health")


class ProxyServiceUser(HttpUser):
    """
    Specialized user for testing proxy service directly.
    Use this class when you want to focus load testing on the proxy service only.
    """
    
    wait_time = between(1, 2)
    
    @task(2)
    def proxy_httpbin(self):
        """Test proxy with httpbin.org"""
        urls_to_test = [
            "https://httpbin.org/json",
            "https://httpbin.org/status/200",
            "https://httpbin.org/delay/1"
        ]
        
        for url in urls_to_test:
            encoded_url = urllib.parse.quote(url, safe='')
            self.client.get(f"/proxy?url={encoded_url}")
    
    @task(1)
    def proxy_health(self):
        """Proxy service health check"""
        self.client.get("/health")