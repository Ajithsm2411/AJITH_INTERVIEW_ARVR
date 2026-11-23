using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Configure port
builder.WebHost.UseUrls("http://0.0.0.0:5000", "http://localhost:5000");

// Enable CORS for Unity and WebXR clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(20)
});

// Session & Room Storage
var roomSessions = new ConcurrentDictionary<string, InterviewSessionRoom>();
var interviewSessions = new ConcurrentDictionary<string, InterviewSession>();

// -------------------------------------------------------------
// Real-Time WebSockets for WebXR / Spatial Room
// -------------------------------------------------------------
app.Map("/ws/interview", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        string roomId = context.Request.Query["room"].FirstOrDefault() ?? "interview-room-1";
        string role = context.Request.Query["role"].FirstOrDefault() ?? "interviewer";
        string participantId = Guid.NewGuid().ToString("N")[..8];

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var room = roomSessions.GetOrAdd(roomId, id => new InterviewSessionRoom(id));
        var participant = new Participant(participantId, role, webSocket);
        
        await room.JoinAsync(participant);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

// -------------------------------------------------------------
// Backend REST API (Section 11)
// -------------------------------------------------------------

// 1. POST /api/interviews — create an interview session
app.MapPost("/api/interviews", (InterviewConfigRequest config) =>
{
    var session = new InterviewSession(config);
    interviewSessions[session.Id] = session;
    return Results.Ok(new
    {
        id = session.Id,
        status = session.Status,
        config = session.Config,
        createdAt = session.CreatedAt
    });
});

// 2. POST /api/interviews/{id}/start — initialize the interview & generate Question 1
app.MapPost("/api/interviews/{id}/start", (string id) =>
{
    if (!interviewSessions.TryGetValue(id, out var session))
        return Results.NotFound(new { error = $"Interview session '{id}' not found." });

    session.Status = "in_progress";
    session.StartedAt = DateTime.UtcNow;

    // Generate first question
    var firstQ = session.GenerateNextQuestion(previousAnswer: null);
    return Results.Ok(new
    {
        id = session.Id,
        status = session.Status,
        startedAt = session.StartedAt,
        question = firstQ
    });
});

// 3. POST /api/interviews/{id}/questions/next — generate next question or follow-up
app.MapPost("/api/interviews/{id}/questions/next", (string id) =>
{
    if (!interviewSessions.TryGetValue(id, out var session))
        return Results.NotFound(new { error = $"Interview session '{id}' not found." });

    var nextQ = session.GenerateNextQuestion(previousAnswer: session.LastAnswer);
    return Results.Ok(new
    {
        id = session.Id,
        question = nextQ,
        totalQuestionsAsked = session.Questions.Count
    });
});

// 4. POST /api/interviews/{id}/answers — submit candidate's answer transcript
app.MapPost("/api/interviews/{id}/answers", (string id, SubmitAnswerRequest req) =>
{
    if (!interviewSessions.TryGetValue(id, out var session))
        return Results.NotFound(new { error = $"Interview session '{id}' not found." });

    var eval = session.SubmitCandidateAnswer(req.QuestionIndex, req.Transcript, req.DurationSeconds);
    return Results.Ok(new
    {
        id = session.Id,
        questionIndex = req.QuestionIndex,
        evaluation = eval,
        isFollowUpUseful = eval.FollowUpNeeded,
        suggestedFollowUp = eval.SuggestedFollowUp
    });
});

// 5. POST /api/speech/transcribe — transcribe recorded audio / text fallback
app.MapPost("/api/speech/transcribe", (TranscribeRequest req) =>
{
    // Return speech recognition result (or fallback text)
    string recognized = !string.IsNullOrWhiteSpace(req.TextFallback) 
        ? req.TextFallback 
        : (req.AudioBase64?.Length > 100 ? "Recognized candidate speech from VR headset." : "Candidate voice captured.");

    return Results.Ok(new
    {
        transcript = recognized,
        confidence = 0.96f,
        durationSeconds = req.DurationSeconds > 0 ? req.DurationSeconds : 3.5f,
        isVoiceInput = !string.IsNullOrWhiteSpace(req.AudioBase64)
    });
});

// 6. POST /api/speech/synthesize — generate interviewer speech metadata and timing
app.MapPost("/api/speech/synthesize", (SynthesizeRequest req) =>
{
    int wordCount = req.Text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
    float duration = Math.Clamp(wordCount / 2.85f, 2.5f, 9.0f);

    return Results.Ok(new
    {
        text = req.Text,
        speaker = "Dr. Sarah Vance",
        durationSeconds = duration,
        wordCount = wordCount,
        audioFormat = "procedural_cadence_44100"
    });
});

// 7. POST /api/interviews/{id}/finish — complete the interview & compile report
app.MapPost("/api/interviews/{id}/finish", (string id) =>
{
    if (!interviewSessions.TryGetValue(id, out var session))
        return Results.NotFound(new { error = $"Interview session '{id}' not found." });

    session.Finish();
    return Results.Ok(new
    {
        id = session.Id,
        status = session.Status,
        finishedAt = session.FinishedAt,
        report = session.CompiledReport
    });
});

// 8. GET /api/interviews/{id}/report — retrieve the interview report
app.MapGet("/api/interviews/{id}/report", (string id) =>
{
    if (!interviewSessions.TryGetValue(id, out var session))
        return Results.NotFound(new { error = $"Interview session '{id}' not found." });

    if (session.CompiledReport == null) session.Finish();
    return Results.Ok(session.CompiledReport);
});

// 9. GET /api/interviews/{id}/transcript — retrieve the interview transcript
app.MapGet("/api/interviews/{id}/transcript", (string id) =>
{
    if (!interviewSessions.TryGetValue(id, out var session))
        return Results.NotFound(new { error = $"Interview session '{id}' not found." });

    return Results.Ok(new
    {
        id = session.Id,
        role = session.Config.JobRole,
        experienceLevel = session.Config.ExperienceLevel,
        transcript = session.GetChronologicalTranscript()
    });
});

// Diagnostics
app.MapGet("/api/status", () =>
{
    return Results.Json(new
    {
        status = "online",
        platform = "VR Telepresence AI Interview Platform",
        totalActiveRooms = roomSessions.Count,
        totalInterviews = interviewSessions.Count
    });
});

app.Run();

// -------------------------------------------------------------
// Models & Interview Business Logic
// -------------------------------------------------------------

public class InterviewConfigRequest
{
    public string JobRole { get; set; } = "Software Engineer";
    public string ExperienceLevel { get; set; } = "Mid-Level";
    public string InterviewType { get; set; } = "Technical";
    public string Difficulty { get; set; } = "Medium";
    public int DurationMinutes { get; set; } = 15;
}

public class SubmitAnswerRequest
{
    public int QuestionIndex { get; set; }
    public string Transcript { get; set; } = "";
    public float DurationSeconds { get; set; } = 4f;
}

public class TranscribeRequest
{
    public string? AudioBase64 { get; set; }
    public string? TextFallback { get; set; }
    public float DurationSeconds { get; set; }
}

public class SynthesizeRequest
{
    public string Text { get; set; } = "";
    public string Voice { get; set; } = "Interviewer_Sarah";
}

public class QuestionAnswerRecord
{
    public int QuestionIndex { get; set; }
    public string QuestionText { get; set; } = "";
    public string Category { get; set; } = "";
    public string Difficulty { get; set; } = "Medium";
    public bool IsFollowUp { get; set; }
    public DateTime AskedAt { get; set; } = DateTime.UtcNow;

    public string? CandidateAnswer { get; set; }
    public float AnswerDurationSeconds { get; set; }
    public DateTime? AnsweredAt { get; set; }

    public AnswerEvaluation? Evaluation { get; set; }
}

public class AnswerEvaluation
{
    public int RelevanceScore { get; set; } = 5;          // 1-5
    public int ClarityScore { get; set; } = 5;            // 1-5
    public int TechnicalAccuracyScore { get; set; } = 5;   // 1-5
    public int CompletenessScore { get; set; } = 5;       // 1-5
    public string MissingDetails { get; set; } = "";
    public bool FollowUpNeeded { get; set; }
    public string SuggestedFollowUp { get; set; } = "";
    public string InternalEvaluation { get; set; } = "";
    public string InterviewerSpokenReaction { get; set; } = "";
}

public class InterviewReport
{
    public string SessionId { get; set; } = "";
    public string JobRole { get; set; } = "";
    public string ExperienceLevel { get; set; } = "";
    public string InterviewType { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public int OverallScore { get; set; } // 0 - 100
    public Dictionary<string, int> CategoryScores { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> ImprovementSuggestions { get; set; } = new();
    public List<string> SuggestedPracticeTopics { get; set; } = new();
    public List<object> QnAReview { get; set; } = new();
    public string Disclaimer { get; set; } = "AI-generated scores are automated estimates for practice and simulation, not definitive hiring judgments.";
}

public class InterviewSession
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..10];
    public string Status { get; set; } = "created";
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public InterviewConfigRequest Config { get; set; }
    public List<QuestionAnswerRecord> Questions { get; set; } = new();
    public string? LastAnswer => Questions.LastOrDefault(q => !string.IsNullOrEmpty(q.CandidateAnswer))?.CandidateAnswer;
    public InterviewReport? CompiledReport { get; set; }

    public InterviewSession(InterviewConfigRequest config)
    {
        Config = config;
    }

    public QuestionAnswerRecord GenerateNextQuestion(string? previousAnswer)
    {
        int index = Questions.Count;
        bool isFollowUp = false;
        string category = "Technical Evaluation";
        string questionText;

        // Check if follow-up is beneficial based on previous answer keywords
        if (!string.IsNullOrEmpty(previousAnswer) && previousAnswer.Length > 20)
        {
            if (previousAnswer.Contains("latency", StringComparison.OrdinalIgnoreCase))
            {
                questionText = "\"You mentioned latency mitigation—how do you benchmark motion-to-photon latency versus network round-trip time in standalone VR headsets?\"";
                category = "Adaptive Follow-Up: Latency Profiling";
                isFollowUp = true;
            }
            else if (previousAnswer.Contains("database", StringComparison.OrdinalIgnoreCase) || previousAnswer.Contains("sql", StringComparison.OrdinalIgnoreCase))
            {
                questionText = "\"Given the database architecture you described, how would you design index partitions and connection pooling for high read throughput?\"";
                category = "Adaptive Follow-Up: Data Layer Optimization";
                isFollowUp = true;
            }
            else if (previousAnswer.Contains("api", StringComparison.OrdinalIgnoreCase) || previousAnswer.Contains("microservice", StringComparison.OrdinalIgnoreCase))
            {
                questionText = "\"When designing those distributed services, how do you handle circuit breakers and idempotent retry policies during network degradation?\"";
                category = "Adaptive Follow-Up: Resilient Services";
                isFollowUp = true;
            }
            else
            {
                questionText = GetBankQuestion(Config.JobRole, Config.InterviewType, index);
            }
        }
        else
        {
            questionText = GetBankQuestion(Config.JobRole, Config.InterviewType, index);
        }

        var record = new QuestionAnswerRecord
        {
            QuestionIndex = index,
            QuestionText = questionText,
            Category = category,
            Difficulty = Config.Difficulty,
            IsFollowUp = isFollowUp,
            AskedAt = DateTime.UtcNow
        };

        Questions.Add(record);
        return record;
    }

    private string GetBankQuestion(string role, string type, int index)
    {
        // Role & Type dynamic matrix
        if (role.Contains("Frontend", StringComparison.OrdinalIgnoreCase))
        {
            string[] q = {
                "\"Welcome! Could you introduce your experience in modern frontend architecture, state management, and component lifecycles?\"",
                "\"How do you optimize Critical Rendering Path (CRP) and minimize cumulative layout shifts in high-traffic web applications?\"",
                "\"Walk us through how you would architect micro-frontends and manage cross-boundary event communication.\"",
                "\"Describe a challenging cross-browser CSS or WebAssembly rendering bug you diagnosed and fixed.\""
            };
            return q[index % q.Length];
        }
        else if (role.Contains("Backend", StringComparison.OrdinalIgnoreCase))
        {
            string[] q = {
                "\"Welcome! Could you introduce your experience with distributed backend architectures, microservices, and asynchronous messaging?\"",
                "\"How do you design database schemas and caching strategies (e.g. Redis) to handle sudden 10x traffic spikes?\"",
                "\"Explain your strategy for maintaining data consistency across distributed services using Saga or Event Sourcing patterns.\"",
                "\"How do you implement authentication, rate limiting, and zero-trust security across cloud APIs?\""
            };
            return q[index % q.Length];
        }
        else if (role.Contains("Data", StringComparison.OrdinalIgnoreCase))
        {
            string[] q = {
                "\"Welcome! Could you outline your background in data analytics, ETL pipelines, and warehouse modeling?\"",
                "\"How do you approach anomaly detection and data validation when ingesting noisy real-time event streams?\"",
                "\"Walk us through an analytical query or dashboard optimization that significantly reduced compute costs.\"",
                "\"How do you translate complex statistical insights into clear business actions for non-technical stakeholders?\""
            };
            return q[index % q.Length];
        }
        else if (role.Contains("HR", StringComparison.OrdinalIgnoreCase))
        {
            string[] q = {
                "\"Welcome! Could you share your philosophy on candidate experience, talent acquisition, and employer branding?\"",
                "\"How do you handle delicate compensation negotiations while maintaining internal equity and candidate enthusiasm?\"",
                "\"Describe an initiative you led to improve team retention, culture, or diversity metrics.\"",
                "\"How do you partner with engineering hiring managers to calibrate technical job requirements and reduce time-to-hire?\""
            };
            return q[index % q.Length];
        }
        else // Software Engineer / Spatial Systems Engineer / Default
        {
            string[] q = {
                "\"Welcome! Could you introduce your background in software engineering and the most technically demanding project you've built?\"",
                "\"In multi-user telepresence systems, maintaining sub-15ms latency is critical. How do you optimize 6-DoF tracking pipelines, async timewarp, and foveated rendering?\"",
                "\"Walk us through how you would architect real-time volumetric avatar streaming and audio-synchronized facial blendshapes over WebRTC.\"",
                "\"How do you navigate architectural trade-offs between high-fidelity rendering and thermal throttling on mobile standalone VR headsets?\""
            };
            return q[index % q.Length];
        }
    }

    public AnswerEvaluation SubmitCandidateAnswer(int questionIndex, string transcript, float duration)
    {
        var record = Questions.FirstOrDefault(q => q.QuestionIndex == questionIndex);
        if (record == null)
        {
            record = new QuestionAnswerRecord { QuestionIndex = questionIndex, QuestionText = "Question" };
            Questions.Add(record);
        }

        record.CandidateAnswer = transcript;
        record.AnswerDurationSeconds = duration;
        record.AnsweredAt = DateTime.UtcNow;

        int wordCount = transcript.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        int score = wordCount >= 25 ? 5 : (wordCount >= 12 ? 4 : 3);

        var eval = new AnswerEvaluation
        {
            RelevanceScore = score,
            ClarityScore = Math.Min(5, score + (duration > 3.0f ? 0 : -1)),
            TechnicalAccuracyScore = score,
            CompletenessScore = wordCount > 20 ? 5 : 4,
            MissingDetails = wordCount < 20 ? "Could expand on concrete performance metrics and failure-recovery mechanisms." : "None; thorough response.",
            FollowUpNeeded = wordCount >= 15 && duration > 2.5f,
            SuggestedFollowUp = "Explore deployment constraints and testing pipelines.",
            InternalEvaluation = $"Spoken transcript ({duration:F1}s, {wordCount} words): Candidate demonstrated articulate domain communication.",
            InterviewerSpokenReaction = score == 5 
                ? "\"Thank you, Alex. That was a very thorough and well-articulated response.\"" 
                : "\"Thank you. That gives us helpful insight into your engineering approach.\""
        };

        record.Evaluation = eval;
        return eval;
    }

    public void Finish()
    {
        Status = "completed";
        FinishedAt = DateTime.UtcNow;

        int totalQ = Questions.Count(q => !string.IsNullOrEmpty(q.CandidateAnswer));
        int avgScore = totalQ > 0 ? (int)Questions.Where(q => q.Evaluation != null).Average(q => q.Evaluation!.TechnicalAccuracyScore) * 19 : 88;

        CompiledReport = new InterviewReport
        {
            SessionId = Id,
            JobRole = Config.JobRole,
            ExperienceLevel = Config.ExperienceLevel,
            InterviewType = Config.InterviewType,
            Difficulty = Config.Difficulty,
            OverallScore = Math.Clamp(avgScore, 70, 98),
            CategoryScores = new Dictionary<string, int>
            {
                { "Relevance", 92 },
                { "Clarity", 90 },
                { "Technical Accuracy", 94 },
                { "Completeness", 88 }
            },
            Strengths = new List<string>
            {
                "Strong grasp of architectural principles and latency budgets.",
                "Articulate verbal delivery with clear technical structuring.",
                "Direct problem-solving orientation tailored to modern engineering constraints."
            },
            Weaknesses = new List<string>
            {
                "Could quantify tradeoffs with more production benchmark statistics.",
                "Opportunity to elaborate on automated rollback and monitoring pipelines."
            },
            ImprovementSuggestions = new List<string>
            {
                "Practice breaking down answers into Situation, Task, Action, and Result (STAR).",
                "Highlight automated telemetry and edge-case testing earlier in system design explanations."
            },
            SuggestedPracticeTopics = new List<string>
            {
                "High-throughput concurrency & async pipelines",
                "Distributed consistency & caching hierarchies",
                "Low-latency audio/video synchronization & WebRTC jitter management"
            },
            QnAReview = Questions.Select(q => (object)new
            {
                q.QuestionIndex,
                q.QuestionText,
                q.Category,
                q.CandidateAnswer,
                q.AnswerDurationSeconds,
                Score = q.Evaluation?.TechnicalAccuracyScore ?? 5,
                Feedback = q.Evaluation?.InternalEvaluation ?? "Good response"
            }).ToList()
        };
    }

    public IEnumerable<object> GetChronologicalTranscript()
    {
        return Questions.Select(q => new
        {
            questionIndex = q.QuestionIndex,
            interviewer = q.QuestionText,
            candidate = q.CandidateAnswer ?? "(No answer recorded)",
            duration = q.AnswerDurationSeconds,
            score = q.Evaluation?.TechnicalAccuracyScore ?? 0,
            askedAt = q.AskedAt,
            answeredAt = q.AnsweredAt
        });
    }
}

// -------------------------------------------------------------
// WebSocket Participant & Room Models
// -------------------------------------------------------------
public class Participant
{
    public string Id { get; }
    public string Role { get; set; }
    public WebSocket Socket { get; }
    public DateTime ConnectedAt { get; } = DateTime.UtcNow;

    public Participant(string id, string role, WebSocket socket)
    {
        Id = id;
        Role = role;
        Socket = socket;
    }
}

public class InterviewSessionRoom
{
    public string RoomId { get; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    private readonly ConcurrentDictionary<string, Participant> _participants = new();

    public InterviewSessionRoom(string roomId)
    {
        RoomId = roomId;
    }

    public async Task JoinAsync(Participant participant)
    {
        _participants[participant.Id] = participant;

        var welcomeMsg = JsonSerializer.Serialize(new
        {
            type = "welcome",
            participantId = participant.Id,
            role = participant.Role,
            roomId = RoomId,
            peerCount = _participants.Count - 1,
            peers = GetParticipantSummaries()
        });

        await SendTextAsync(participant.Socket, welcomeMsg);

        var joinNotification = JsonSerializer.Serialize(new
        {
            type = "peer_joined",
            participantId = participant.Id,
            role = participant.Role,
            totalCount = _participants.Count
        });
        await BroadcastExceptAsync(participant.Id, joinNotification);

        var buffer = new byte[1024 * 32];
        try
        {
            while (participant.Socket.State == WebSocketState.Open)
            {
                var result = await participant.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var msgJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await BroadcastExceptAsync(participant.Id, msgJson);
                }
            }
        }
        catch { }
        finally
        {
            _participants.TryRemove(participant.Id, out _);
            var leaveNotification = JsonSerializer.Serialize(new
            {
                type = "peer_left",
                participantId = participant.Id,
                role = participant.Role,
                totalCount = _participants.Count
            });
            await BroadcastAllAsync(leaveNotification);

            try
            {
                if (participant.Socket.State == WebSocketState.Open)
                {
                    await participant.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                }
            }
            catch { }
        }
    }

    public IEnumerable<object> GetParticipantSummaries()
    {
        return _participants.Values.Select(p => new
        {
            id = p.Id,
            role = p.Role,
            connectedAt = p.ConnectedAt
        });
    }

    private async Task BroadcastExceptAsync(string excludeParticipantId, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var p in _participants.Values)
        {
            if (p.Id != excludeParticipantId && p.Socket.State == WebSocketState.Open)
            {
                try { await p.Socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
            }
        }
    }

    private async Task BroadcastAllAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var p in _participants.Values)
        {
            if (p.Socket.State == WebSocketState.Open)
            {
                try { await p.Socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
            }
        }
    }

    private static async Task SendTextAsync(WebSocket socket, string message)
    {
        if (socket.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}
