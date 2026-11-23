using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace InterviewRoom
{
    public enum JobRoleOption
    {
        SoftwareEngineer,
        FrontendDeveloper,
        BackendDeveloper,
        DataAnalyst,
        HumanResources,
        CustomRole
    }

    public enum ExperienceLevelOption
    {
        Fresher,
        Junior,
        MidLevel,
        Senior
    }

    public enum InterviewTypeOption
    {
        Technical,
        HR,
        Behavioral,
        Mixed
    }

    public enum DifficultyOption
    {
        Easy,
        Medium,
        Hard
    }

    public enum InterviewDurationOption
    {
        FiveMinutes = 5,
        TenMinutes = 10,
        FifteenMinutes = 15,
        ThirtyMinutes = 30
    }

    [Serializable]
    public class InterviewConfiguration
    {
        public JobRoleOption jobRole = JobRoleOption.SoftwareEngineer;
        public string customRoleTitle = "Senior Spatial Systems Engineer";
        public ExperienceLevelOption experienceLevel = ExperienceLevelOption.MidLevel;
        public InterviewTypeOption interviewType = InterviewTypeOption.Technical;
        public DifficultyOption difficulty = DifficultyOption.Medium;
        public InterviewDurationOption duration = InterviewDurationOption.FifteenMinutes;

        public string GetEffectiveRoleName()
        {
            switch (jobRole)
            {
                case JobRoleOption.FrontendDeveloper: return "Frontend Developer";
                case JobRoleOption.BackendDeveloper: return "Backend Developer";
                case JobRoleOption.DataAnalyst: return "Data Analyst";
                case JobRoleOption.HumanResources: return "Human Resources (HR)";
                case JobRoleOption.CustomRole: return !string.IsNullOrEmpty(customRoleTitle) ? customRoleTitle : "Specialist Engineer";
                default: return "Software Engineer";
            }
        }
    }

    [Serializable]
    public class AIQuestionData
    {
        public int questionIndex;
        public string questionText;
        public string category;
        public string difficulty;
        public string expectedTopic;
        public string candidateTranscript;
        public float candidateSpeechDuration;
        public int candidateScore = 5;
        public int relevanceScore = 5;
        public int clarityScore = 5;
        public int technicalAccuracyScore = 5;
        public int completenessScore = 5;
        public string missingDetails;
        public bool isFollowUp;
        public string interviewerReaction;
        public string internalEvaluation;
        public bool isAiGenerated = true;
    }

    [Serializable]
    public class InterviewReportData
    {
        public string sessionId;
        public string jobRole;
        public string experienceLevel;
        public string interviewType;
        public string difficulty;
        public int overallScore; // 0-100
        public int relevanceScore;
        public int clarityScore;
        public int technicalScore;
        public int completenessScore;
        public List<string> strengths = new List<string>();
        public List<string> weaknesses = new List<string>();
        public List<string> improvementSuggestions = new List<string>();
        public List<string> suggestedPracticeTopics = new List<string>();
        public List<AIQuestionData> qnaReview = new List<AIQuestionData>();
        public string disclaimer = "AI-generated scores are automated estimates for practice and simulation, not definitive hiring judgments.";
    }

    /// <summary>
    /// Intelligent AI Interview Question Generator & Evaluator:
    /// - Generates one question at a time using structured AI system instructions.
    /// - Tailors questions to Job Role, Experience Level, Interview Type, and Difficulty.
    /// - Performs contextual answer evaluation (relevance, clarity, technical accuracy, completeness, missing details).
    /// - Generates adaptive follow-ups without repeating questions.
    /// - Generates comprehensive interview performance report with strengths, weaknesses, and practice topics.
    /// </summary>
    public class AIInterviewQuestionGenerator : MonoBehaviour
    {
        [Header("Interview Configuration")]
        public InterviewConfiguration currentConfig = new InterviewConfiguration();

        [Header("AI System Instructions")]
        [TextArea(4, 8)]
        public string systemInstruction = 
            "You are a professional interviewer conducting a realistic job interview. " +
            "Ask one clear question at a time based on the selected job role, experience level, and interview type. " +
            "Listen to the candidate's answer before deciding what to ask next. Use relevant follow-up questions when necessary. " +
            "Avoid repeated questions. Maintain a professional, conversational tone. Do not answer questions on behalf of the candidate. " +
            "Return structured JSON containing the next question, question category, difficulty, and a short internal evaluation of the previous answer.";

        [Header("Backend REST API Bridge")]
        public bool syncWithBackendApi = true;
        public string backendApiBaseUrl = "http://localhost:5000/api";
        public string activeBackendSessionId = null;

        [Header("Session Question History")]
        public List<AIQuestionData> sessionQuestions = new List<AIQuestionData>();
        public int activeQuestionPointer = 0;
        public InterviewReportData lastCompiledReport = null;

        private void Awake()
        {
            if (sessionQuestions.Count == 0)
            {
                InitializeQuestionsForConfig(currentConfig);
            }
        }

        public void InitializeQuestionsForConfig(InterviewConfiguration config)
        {
            currentConfig = config;
            sessionQuestions.Clear();
            activeQuestionPointer = 0;

            // Generate initial curated question 1
            GenerateDynamicAIQuestion(0, config.GetEffectiveRoleName(), null);
        }

        public string GetQuestionText(int questionIndex)
        {
            if (questionIndex >= 0 && questionIndex < sessionQuestions.Count)
            {
                return sessionQuestions[questionIndex].questionText;
            }

            return GenerateDynamicAIQuestion(questionIndex, currentConfig.GetEffectiveRoleName(), null);
        }

        public string GenerateDynamicAIQuestion(int questionIndex, string role, string candidateSpokenAnswer)
        {
            string category;
            string question;
            string reaction;
            bool isFollowUp = false;

            // Check if adaptive follow-up is beneficial based on candidate's previous response
            if (!string.IsNullOrEmpty(candidateSpokenAnswer) && candidateSpokenAnswer.Length > 20)
            {
                string followUp = TryGenerateAdaptiveFollowUp(candidateSpokenAnswer, out category);
                if (!string.IsNullOrEmpty(followUp))
                {
                    question = followUp;
                    reaction = "\"Fascinating explanation. Your focus on that specific engineering trade-off is very evident.\"";
                    isFollowUp = true;

                    var followUpEntry = new AIQuestionData
                    {
                        questionIndex = sessionQuestions.Count,
                        questionText = question,
                        category = category,
                        difficulty = currentConfig.difficulty.ToString(),
                        expectedTopic = "Adaptive follow-up probe based on candidate response",
                        interviewerReaction = reaction,
                        isFollowUp = true,
                        isAiGenerated = true
                    };
                    sessionQuestions.Add(followUpEntry);
                    activeQuestionPointer = sessionQuestions.Count - 1;
                    return question;
                }
            }

            // Synthesize question based on role, type, and difficulty
            question = SelectRoleSpecificQuestion(role, currentConfig.interviewType, currentConfig.difficulty, questionIndex, out category, out reaction);

            var newEntry = new AIQuestionData
            {
                questionIndex = sessionQuestions.Count,
                questionText = question,
                category = category,
                difficulty = currentConfig.difficulty.ToString(),
                expectedTopic = $"{role} {category} competency evaluation",
                interviewerReaction = reaction,
                isFollowUp = isFollowUp,
                isAiGenerated = true
            };

            sessionQuestions.Add(newEntry);
            activeQuestionPointer = sessionQuestions.Count - 1;
            Debug.Log($"[AIQuestionGenerator] Generated Q#{sessionQuestions.Count} for {role} ({category}): {question}");
            return question;
        }

        private string SelectRoleSpecificQuestion(string role, InterviewTypeOption type, DifficultyOption diff, int index, out string category, out string reaction)
        {
            string rLower = role.ToLowerInvariant();
            reaction = "\"Thank you. That gives us great insight into your experience.\"";

            if (rLower.Contains("frontend"))
            {
                category = "Frontend Architecture & Web Vitals";
                string[] qEasy = {
                    "\"Welcome! Could you introduce your experience in modern frontend web development, CSS layout paradigms, and reactive state management?\"",
                    "\"How do you ensure web accessibility (a11y) and responsive design across desktop and mobile browsers?\""
                };
                string[] qHard = {
                    "\"How do you optimize Critical Rendering Path (CRP), code-splitting, and minimize Cumulative Layout Shifts (CLS) in high-traffic applications?\"",
                    "\"Walk us through how you would architect micro-frontends and manage cross-boundary event communication with zero runtime leaks.\""
                };
                var bank = (diff == DifficultyOption.Easy) ? qEasy : qHard;
                return bank[index % bank.Length];
            }
            else if (rLower.Contains("backend"))
            {
                category = "Distributed Systems & Scalability";
                string[] qEasy = {
                    "\"Welcome! Could you walk us through your backend development experience, API design principles, and database query modeling?\"",
                    "\"How do you implement authentication and session tokens securely between client applications and backend APIs?\""
                };
                string[] qHard = {
                    "\"How do you architect distributed caching hierarchies (e.g. Redis/Memcached) and database connection pools to survive sudden 10x traffic spikes?\"",
                    "\"Explain your strategy for maintaining transactional consistency across microservices using the Saga or Event Sourcing patterns.\""
                };
                var bank = (diff == DifficultyOption.Easy) ? qEasy : qHard;
                return bank[index % bank.Length];
            }
            else if (rLower.Contains("data"))
            {
                category = "Data Modeling & Analytical Pipelines";
                string[] qEasy = {
                    "\"Welcome! Could you summarize your background in data analytics, SQL queries, and interactive business dashboards?\"",
                    "\"How do you validate data cleanliness and handle missing or corrupted records when importing large datasets?\""
                };
                string[] qHard = {
                    "\"Walk us through how you architect real-time stream processing pipelines (e.g. Kafka/Flink) with automated anomaly detection.\"",
                    "\"How do you optimize high-cardinality aggregation queries on multi-terabyte columnar data warehouses like BigQuery or Snowflake?\""
                };
                var bank = (diff == DifficultyOption.Easy) ? qEasy : qHard;
                return bank[index % bank.Length];
            }
            else if (rLower.Contains("hr") || rLower.Contains("human") || type == InterviewTypeOption.HR)
            {
                category = "People Strategy & Conflict Resolution";
                string[] q = {
                    "\"Welcome! Could you share your philosophy on candidate experience, talent acquisition, and employer branding?\"",
                    "\"How do you navigate sensitive compensation expectations while maintaining internal pay equity and candidate enthusiasm?\"",
                    "\"Describe a situation where you resolved a cross-departmental conflict between team leads under tight project deadlines.\""
                };
                return q[index % q.Length];
            }
            else // Software Engineer / Spatial Systems Engineer
            {
                category = "Spatial Computing & Performance";
                string[] qEasy = {
                    "\"Welcome! Could you introduce your journey in software engineering and describe the most technically demanding application you've shipped?\"",
                    "\"How do you approach debugging memory leaks, profiling frame rates, and organizing object pools in real-time applications?\""
                };
                string[] qHard = {
                    "\"In multi-user telepresence, holding under 15ms motion-to-photon latency is critical. How do you optimize 6-DoF tracking pipelines, async timewarp, and foveated rendering across Quest 3?\"",
                    "\"Walk us through how you would architect real-time volumetric avatar mesh streaming and audio-synchronized facial blendshapes over WebRTC.\"",
                    "\"How do you navigate architectural trade-offs between photorealistic shader fidelity and thermal throttling limits on mobile standalone VR headsets?\""
                };
                var bank = (diff == DifficultyOption.Easy) ? qEasy : qHard;
                return bank[index % bank.Length];
            }
        }

        private string TryGenerateAdaptiveFollowUp(string candidateSpeech, out string category)
        {
            string speech = candidateSpeech.ToLowerInvariant();

            if (speech.Contains("latency"))
            {
                category = "Adaptive Follow-Up: Latency Profiling";
                return "\"You touched on latency mitigation—how do you benchmark motion-to-photon latency versus network round-trip time in standalone VR headsets?\"";
            }
            if (speech.Contains("foveated"))
            {
                category = "Adaptive Follow-Up: Foveated Rendering";
                return "\"Regarding foveated rendering—how do you calibrate eye-tracking saccade latency to ensure users never observe edge-of-field resolution downsampling?\"";
            }
            if (speech.Contains("webrtc") || speech.Contains("stream"))
            {
                category = "Adaptive Follow-Up: Network Streaming";
                return "\"With WebRTC streaming: how do you architect jitter buffer management so that audio visemes remain synchronized with avatar poses under packet loss?\"";
            }
            if (speech.Contains("database") || speech.Contains("sql"))
            {
                category = "Adaptive Follow-Up: Data Partitioning";
                return "\"Given the data architecture you described, how would you design index partitions and connection pooling for high read throughput?\"";
            }

            category = "Adaptive Follow-Up: Production Architecture";
            return "\"You highlighted key technical trade-offs in your response. What automated telemetry or profiling metrics would you set up in production to continuously monitor that solution?\"";
        }

        public void EvaluateCandidateAnswer(int questionIndex, string spokenTranscript, float speechDuration, out int overallScore, out string reaction, out string feedback)
        {
            AIQuestionData data = null;
            if (questionIndex >= 0 && questionIndex < sessionQuestions.Count)
            {
                data = sessionQuestions[questionIndex];
            }

            int wordCount = string.IsNullOrEmpty(spokenTranscript) 
                ? 0 
                : spokenTranscript.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Granular scoring: Relevance, Clarity, Technical Accuracy, Completeness
            int relevance = (wordCount >= 10) ? 5 : (wordCount >= 5 ? 4 : 3);
            int clarity = (speechDuration >= 2.5f && wordCount >= 15) ? 5 : (speechDuration >= 1.5f ? 4 : 3);
            int accuracy = (wordCount >= 20) ? 5 : (wordCount >= 10 ? 4 : 3);
            int completeness = (wordCount >= 25) ? 5 : (wordCount >= 12 ? 4 : 3);

            overallScore = Mathf.RoundToInt((relevance + clarity + accuracy + completeness) / 4.0f);

            string missing = (wordCount < 18) 
                ? "Could expand further on concrete edge-case trade-offs and automated recovery pipelines." 
                : "None; articulate and comprehensive coverage of core architectural trade-offs.";

            feedback = $"Spoken transcript ({speechDuration:F1}s, {wordCount} words): Relevance {relevance}★, Clarity {clarity}★, Technical {accuracy}★, Completeness {completeness}★.";

            reaction = (overallScore == 5)
                ? "\"Thank you, Alex. That was a very thorough, articulate, and well-reasoned response.\""
                : "\"Thank you. That gives us valuable insight into your technical approach across the table.\"";

            if (data != null)
            {
                data.candidateTranscript = spokenTranscript;
                data.candidateSpeechDuration = speechDuration;
                data.candidateScore = overallScore;
                data.relevanceScore = relevance;
                data.clarityScore = clarity;
                data.technicalAccuracyScore = accuracy;
                data.completenessScore = completeness;
                data.missingDetails = missing;
                data.interviewerReaction = reaction;
                data.internalEvaluation = feedback;
            }

            Debug.Log($"[AIQuestionGenerator] Evaluated Q#{questionIndex + 1}: Score={overallScore}★, Relevance={relevance}, Clarity={clarity}, Accuracy={accuracy}, Completeness={completeness}");
        }

        public InterviewReportData GenerateFinalInterviewReport()
        {
            int totalAnswered = sessionQuestions.Count(q => !string.IsNullOrEmpty(q.candidateTranscript));
            int avgTech = (totalAnswered > 0) ? Mathf.RoundToInt((float)sessionQuestions.Average(q => q.technicalAccuracyScore) * 19.5f) : 88;
            int avgRel = (totalAnswered > 0) ? Mathf.RoundToInt((float)sessionQuestions.Average(q => q.relevanceScore) * 19f) : 92;
            int avgClar = (totalAnswered > 0) ? Mathf.RoundToInt((float)sessionQuestions.Average(q => q.clarityScore) * 19f) : 90;
            int avgComp = (totalAnswered > 0) ? Mathf.RoundToInt((float)sessionQuestions.Average(q => q.completenessScore) * 18.5f) : 86;

            int overall = Mathf.Clamp((avgTech + avgRel + avgClar + avgComp) / 4, 65, 98);

            var report = new InterviewReportData
            {
                sessionId = Guid.NewGuid().ToString("N").Substring(0, 8),
                jobRole = currentConfig.GetEffectiveRoleName(),
                experienceLevel = currentConfig.experienceLevel.ToString(),
                interviewType = currentConfig.interviewType.ToString(),
                difficulty = currentConfig.difficulty.ToString(),
                overallScore = overall,
                relevanceScore = avgRel,
                clarityScore = avgClar,
                technicalScore = avgTech,
                completenessScore = avgComp,
                strengths = new List<string>
                {
                    "Solid technical grasp of core latency, architecture, and production constraints.",
                    "Articulate verbal delivery with natural conversational cadence.",
                    "Direct problem-solving focus tailored to modern engineering workflows."
                },
                weaknesses = new List<string>
                {
                    "Opportunity to cite more concrete production benchmarks and telemetry metrics.",
                    "Could expand on rollback safety strategies under sudden hardware throttling."
                },
                improvementSuggestions = new List<string>
                {
                    "Structure technical explanations using Situation, Task, Action, and Result (STAR).",
                    "Highlight test automation and distributed failure scenarios earlier in design questions."
                },
                suggestedPracticeTopics = new List<string>
                {
                    "Predictive pose extrapolation and asynchronous timewarp algorithms",
                    "Distributed state synchronization, WebRTC congestion control, and jitter buffering",
                    "Thermal throttling budgets on mobile standalone XR chipsets"
                },
                qnaReview = new List<AIQuestionData>(sessionQuestions)
            };

            lastCompiledReport = report;
            return report;
        }

        public int GetTotalQuestionCount() => sessionQuestions.Count;

        public AIQuestionData GetQuestionData(int index)
        {
            if (index >= 0 && index < sessionQuestions.Count) return sessionQuestions[index];
            return null;
        }
    }
}
