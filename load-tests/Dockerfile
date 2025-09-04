FROM locustio/locust

# Copy locust files to container
COPY locustfile.py /mnt/locust/
COPY requirements.txt /mnt/locust/
COPY locust.conf /mnt/locust/

# Install additional requirements if any
RUN pip3 install -r /mnt/locust/requirements.txt

# Set working directory
WORKDIR /mnt/locust

# Default command - use config file for settings
CMD ["--config", "/mnt/locust/locust.conf"]