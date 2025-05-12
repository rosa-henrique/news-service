# 📚 NewsService Documentation

## 📦 Project Structure

The project is composed of several key components. **NewsService.Web** serves as the Blazor-based frontend for user interaction. The backend logic is handled by **NewsService.Api**, which processes HTTP/gRPC requests, interacts with Postgres and MinIO for data storage, and publishes messages to the messaging system. Messaging contracts, such as `ProcessNewsFiles` and `ProcessFiles`, are defined in **NewsService.Contracts**. Data persistence with PostgreSQL is managed by **NewsService.Postgres**, which includes EF Core models and a DbContext utilizing Data Annotations. Database migrations are applied at startup by the **NewsService.Migrations** worker. The **NewsService.ProcessFile** component is responsible for moving files between different MinIO buckets. Finally, **NewsService.SyncDatabase** ensures that status information and logs are synchronized between Postgres and Elasticsearch.

## 🧰 Technologies Used

The NewsService leverages a modern technology stack to deliver its functionalities. Orchestration and composition of services are managed by .NET Aspire. MassTransit, in conjunction with RabbitMQ, provides a robust messaging infrastructure. PostgreSQL is employed for relational data storage, accessed via EF Core with Data Annotations. File object storage is handled by MinIO. Elasticsearch is utilized for tracking and maintaining audit logs. Communication between the Blazor frontend and the backend is facilitated by gRPC, ensuring efficient and strongly-typed interactions.

## 🔁 Messaging & Processing Flow

The messaging and processing flow begins when a `ProcessNewsFiles` message is published, containing a list of files to be processed and the index of the current file. The `ProcessNewsFilesConsumer` takes responsibility for moving the current file to its designated destination bucket and then continues the processing flow. Subsequently, the `NewsProcessingConsumer` updates both Postgres and Elasticsearch with the relevant statuses and any error messages encountered during the process. The finalization of the entire operation is inferred when the `CurrentFile` index surpasses the total number of files in the list.

## 📁 File Processing Strategy

Files within the NewsService are processed **sequentially**. This is achieved by recursively pushing each individual processing step back into the orchestrator queue. This sequential strategy offers several advantages. It enables clear and straightforward retry logic for failed steps. Furthermore, it allows for the independent tracking of each file's processing stage. This approach also provides a mechanism for graceful fallback, ensuring that if one file type, such as a video, encounters a processing failure, it does not halt the processing of other files.

## 🔍 Tracking & Logs

Comprehensive tracking and logging are implemented within the system. A single `news_state` document is maintained to track the overall processing status of a news item. In addition, `news_state_audit` stores a detailed history of each individual status change, complete with timestamps. These status updates and logs are consistently recorded by the `NewsProcessingConsumer` at every step of the processing workflow.

## 🗄️ Data Model

### News

The core data model revolves around a "News" entity, which can be associated with different types of files.
```
News
├── Document (DocumentFile)
├── Image (ImageFile)
└── Video (VideoFile)
```
Each specific file type, such as documents, images, and videos, is stored in its own respective table within the database: `news_documents_file`, `news_images_file`, and `news_videos_file`. These specific file tables extend a common base `NewsFile` class, promoting a structured and organized data schema.

## 📊 Elasticsearch Indexes

Two primary Elasticsearch indexes are utilized for managing and querying data. The **news_state** index represents the current processing state for any given news item, providing a real-time overview. The **news_state_audit** index, on the other hand, tracks the detailed history of status changes for each file, offering a comprehensive audit trail.

## 🔌 gRPC Communication

Communication between the frontend and the backend of the NewsService is established using **gRPC**. This choice allows for structured, efficient, and strongly-typed service calls, enhancing the reliability and performance of data exchange between the two tiers.

## 📬 Queue Strategy with MassTransit

MassTransit is employed to manage the queuing strategy, and it automatically names queues and exchanges based on the class names involved. The queue pattern is designed for clarity and efficiency. The `ProcessNewsFiles` message type is central to this strategy, being used throughout the entire lifecycle of file processing. Furthermore, the use of class-based exchanges and queues facilitates a clear separation of processing responsibilities among different consumers.

## ✅ Status Handling

Status handling is a critical aspect of the processing workflow, with status enums being updated by the various consumers involved. For individual files, the status can be `Pending`, `Completed`, or `Failed`, reflecting the current stage of processing. The tracking type is typically marked as `Created` upon initiation. The overall state of a news item can be `ProcessingInit` when starting, `Success` upon successful completion of all files, or `Failed` if any part of the process encounters an unrecoverable error.

