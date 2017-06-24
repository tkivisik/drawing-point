### NOTES
# * Image analysis and OCR is working in Python 2.7, Linux Mint.

### Sample Code From:
# Image Analysis ==>
# https://westus.dev.cognitive.microsoft.com/docs/services/56f91f2d778daf23d8ec6739/operations/56f91f2e778daf14a499e1fa
# OCR ==> 
# https://westus.dev.cognitive.microsoft.com/docs/services/56f91f2d778daf23d8ec6739/operations/56f91f2e778daf14a499e1fc

########### Python 2.7 #############
import httplib, urllib, base64

# Image from Wikipedia, from a user Nakamura2828 . Thank you.
# Read more: https://commons.wikimedia.org/wiki/Category:Illustrations_in_English#/media/File:Panel_door.jpg
image_url = "https://upload.wikimedia.org/wikipedia/commons/b/b4/Panel_door.jpg"
image_request = "{'url':'" + image_url + "'}"
with open("secret_subscription_key.txt") as f:
#    secret_subscription_key = '{' + f.readline().rstrip() + '}'
    secret_subscription_key = f.readline().rstrip()

headers = {
    # Request headers
    'Content-Type': 'application/json',
    'Ocp-Apim-Subscription-Key': secret_subscription_key,
}

params = urllib.urlencode({
    # Request parameters
    'visualFeatures': 'Categories,Description',
    'language': 'en',
})

# Image Analysis
print("#"*80)
print("IMAGE ANALYSIS STARTS HERE")
try:
    conn = httplib.HTTPSConnection('westeurope.api.cognitive.microsoft.com')
    conn.request("POST", "/vision/v1.0/analyze?%s" % params, image_request, headers)
    response = conn.getresponse()
    data = response.read()
    print(data)
    conn.close()
except Exception as e:
    print("[Errno {0}] {1}".format(e.errno, e.strerror))


print("\n" + "#"*80)
print("OCR STARTS HERE")


####################################

params = urllib.urlencode({
    # Request parameters
    'language': 'unk',
    'detectOrientation ': 'true',
})

print
try:
    conn = httplib.HTTPSConnection('westeurope.api.cognitive.microsoft.com')
    conn.request("POST", "/vision/v1.0/ocr?%s" % params, image_request, headers)
#    conn.request("POST", "/vision/v1.0/analyze?%s" % params, image_request, headers)
    response = conn.getresponse()
    data = response.read()
    print(data)
    conn.close()
except Exception as e:
    print("[Errno {0}] {1}".format(e.errno, e.strerror))