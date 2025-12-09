# Task_ImageManager

ASP.NET Core Web API for downloading and storing images from URLs locally.

## API Endpoints

### 1. POST `/Images`
**Download and Store Images**

Downloads multiple images from provided URLs and stores them locally on the server.

**Request Body:**
```json
{
  "imageUrls": ["https://example.com/image1.jpg", "https://example.com/image2.png"],
  "maxDownloadAtOnce": 5
}
```

**Response:**
```json
{
  "success": true,
  "message": "2 downloaded successfully.",
  "urlAndNames": {
    "https://example.com/image1.jpg": "stored_filename_1.jpg",
    "https://example.com/image2.png": "stored_filename_2.png"
  }
}
```

**Behavior:**
- Downloads images concurrently (controlled by `maxDownloadAtOnce`)
- Stores images locally with generated filenames
- Returns mapping of original URLs to stored filenames

---

### 2. GET `/Images/get-image-by-name/{imageName}`
**Retrieve Stored Image**

Retrieves a previously stored image by its filename and returns it as a Base64 encoded string.

**Parameters:**
- `imageName` (route): The filename of the stored image

**Response (Success):**
```json
"data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEA..."
```

**Response (Error):**
```json
"Image name is required."
```
or
```json
"Image not found."
```

**Behavior:**
- Validates that image name is provided
- Retrieves the image from local storage
- Returns Base64 encoded image data with appropriate MIME type prefix
- Returns HTTP 400 (Bad Request) if image name is invalid or image not found

---

## Testing

A Postman collection is provided in the `PostmanCollection` folder for easy API testing and exploration.
