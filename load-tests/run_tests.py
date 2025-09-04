#!/usr/bin/env python3
"""
Helper script to run different load test scenarios against Hermes services.
"""

import subprocess
import sys
import time
import argparse
from urllib.parse import urlparse
import requests


def check_service_health(host, endpoint="/health", timeout=5):
    """Check if a service is healthy before starting load tests."""
    try:
        url = f"{host}{endpoint}"
        response = requests.get(url, timeout=timeout)
        return response.status_code == 200
    except requests.RequestException:
        return False


def wait_for_services(hosts, max_wait=30):
    """Wait for services to be healthy before proceeding."""
    print("Checking service health...")
    
    for host in hosts:
        print(f"Waiting for {host} to be healthy...")
        waited = 0
        while waited < max_wait:
            if check_service_health(host):
                print(f"✓ {host} is healthy")
                break
            time.sleep(2)
            waited += 2
        else:
            print(f"✗ {host} is not responding after {max_wait}s")
            return False
    
    print("All services are healthy!")
    return True


def run_load_test(scenario, host, users=10, spawn_rate=2, run_time="60s", user_class=None):
    """Run a specific load test scenario."""
    
    cmd = [
        "locust", 
        "--headless",
        "--users", str(users),
        "--spawn-rate", str(spawn_rate), 
        "--run-time", run_time,
        "--host", host,
        "--csv", f"results/{scenario}",
        "--html", f"results/{scenario}.html"
    ]
    
    if user_class:
        cmd.extend(["-f", "locustfile.py", user_class])
    
    print(f"\nRunning {scenario} load test...")
    print(f"Command: {' '.join(cmd)}")
    
    # Create results directory
    subprocess.run(["mkdir", "-p", "results"], check=False)
    
    try:
        result = subprocess.run(cmd, check=True, capture_output=True, text=True)
        print(f"✓ {scenario} test completed successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"✗ {scenario} test failed: {e}")
        print(f"Error output: {e.stderr}")
        return False


def main():
    parser = argparse.ArgumentParser(description="Run Hermes load tests")
    parser.add_argument("--scenario", choices=["all", "api", "proxy", "web", "quick"], 
                        default="quick", help="Test scenario to run")
    parser.add_argument("--users", type=int, default=10, help="Number of concurrent users")
    parser.add_argument("--spawn-rate", type=int, default=2, help="Users to spawn per second")
    parser.add_argument("--run-time", default="60s", help="Test duration (e.g., 60s, 5m)")
    parser.add_argument("--skip-health-check", action="store_true", help="Skip service health checks")
    
    args = parser.parse_args()
    
    # Service endpoints (adjust ports based on your Aspire configuration)
    services = {
        "web": "http://localhost:5000",      # Web frontend
        "proxy": "http://localhost:5001",    # Proxy service  
        "api": "http://localhost:5002"       # API service
    }
    
    # Check service health unless skipped
    if not args.skip_health_check:
        hosts_to_check = list(services.values())
        if not wait_for_services(hosts_to_check):
            print("Some services are not healthy. Use --skip-health-check to proceed anyway.")
            return 1
    
    success_count = 0
    total_tests = 0
    
    if args.scenario == "quick":
        # Quick smoke test
        total_tests = 1
        if run_load_test("quick_smoke", services["web"], 
                        users=5, spawn_rate=1, run_time="30s"):
            success_count += 1
            
    elif args.scenario == "api":
        # API service focused testing
        total_tests = 1
        if run_load_test("api_focused", services["api"], 
                        args.users, args.spawn_rate, args.run_time, "ApiServiceUser"):
            success_count += 1
            
    elif args.scenario == "proxy":
        # Proxy service focused testing
        total_tests = 1
        if run_load_test("proxy_focused", services["proxy"],
                        args.users, args.spawn_rate, args.run_time, "ProxyServiceUser"):
            success_count += 1
            
    elif args.scenario == "web":
        # Web frontend comprehensive testing
        total_tests = 1
        if run_load_test("web_comprehensive", services["web"],
                        args.users, args.spawn_rate, args.run_time, "HermesUser"):
            success_count += 1
            
    elif args.scenario == "all":
        # Run all scenarios
        scenarios = [
            ("api_service", services["api"], "ApiServiceUser"),
            ("proxy_service", services["proxy"], "ProxyServiceUser"), 
            ("web_comprehensive", services["web"], "HermesUser")
        ]
        
        total_tests = len(scenarios)
        for name, host, user_class in scenarios:
            if run_load_test(name, host, args.users, args.spawn_rate, args.run_time, user_class):
                success_count += 1
    
    # Summary
    print(f"\n{'='*50}")
    print(f"Load Test Summary: {success_count}/{total_tests} tests passed")
    print(f"Results saved in: results/ directory")
    print(f"{'='*50}")
    
    return 0 if success_count == total_tests else 1


if __name__ == "__main__":
    sys.exit(main())