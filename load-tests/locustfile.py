from locust import HttpUser, task, between
import urllib.parse


class ProxyWeatherUser(HttpUser):
    """
    Load test user focused on testing proxy calls to the weather service only.
    """
    
    wait_time = between(1, 3)
    
    @task
    def test_proxy_weather(self):
        """Test proxy calling weather service via Aspire service discovery"""
        internal_url = "https+http://apiservice/weatherforecast"
        encoded_url = urllib.parse.quote(internal_url, safe='')
        
        with self.client.get(f"/proxy?url={encoded_url}", catch_response=True) as response:
            if response.status_code == 200:
                response.success()
            else:
                response.failure(f"Proxy weather call failed with status {response.status_code}")