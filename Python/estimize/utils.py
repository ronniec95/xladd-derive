from requests import post, get
from requests.exceptions import RequestException
from contextlib import closing
import random


def log_error(msg):
    print(msg)


def download_get(url):
    UAS = ("Mozilla/5.0 (Windows NT 6.1; WOW64; rv:40.0) Gecko/20100101 Firefox/40.1",
           "Mozilla/5.0 (Windows NT 6.3; rv:36.0) Gecko/20100101 Firefox/36.0",
           "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10; rv:33.0) Gecko/20100101 Firefox/33.0",
           "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36",
           "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_10_1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2227.1 Safari/537.36",
           "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2227.0 Safari/537.36",
           )

    ua = UAS[random.randrange(len(UAS))]
    try:
        with closing(get(url, params={"Upgrade-Insecure-Requests": 1}, headers={"user-agent": ua})) as resp:
            if is_ok_response(resp):
                return resp.content
            else:
                return None
    except RequestException as e:
        log_error("Error during requests to {} {}".format(url, str(e)))
        return None


def download_post(url, data):
    try:
        with closing(post(url, data=data)) as resp:
            if is_ok_response(resp):
                return resp.content
            else:
                return None
    except RequestException as e:
        log_error("Error during requests to {} {}".format(url, str(e)))
        return None


def is_ok_response(resp):
    content_type = resp.headers["Content-Type"].lower()
    return (resp.status_code == 200 and content_type is not None)


def read(name):
    with open("{}.html".format(name), "r") as f:
        return f.read()


def write(name, content):
    with open('{}.html'.format(name), 'w') as f:
        f.write(content)


def write_json(name, content):
    with open('{}.json'.format(name), 'w') as f:
        f.write(content)
