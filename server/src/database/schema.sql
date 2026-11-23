-- ============================================================================
-- VR Telepresence AI Interview Platform — SQLite Database Schema
-- Spec Section 12: Database Design
-- ============================================================================

PRAGMA foreign_keys = ON;

-- 1. Users Table
CREATE TABLE IF NOT EXISTS Users (
    id VARCHAR(64) PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    role VARCHAR(64) DEFAULT 'candidate',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. InterviewSessions Table
CREATE TABLE IF NOT EXISTS InterviewSessions (
    id VARCHAR(64) PRIMARY KEY,
    user_id VARCHAR(64) NOT NULL,
    job_role VARCHAR(128) NOT NULL,
    custom_role_title VARCHAR(255),
    experience_level VARCHAR(64) NOT NULL,
    interview_type VARCHAR(64) NOT NULL,
    difficulty VARCHAR(32) NOT NULL,
    duration_minutes INTEGER NOT NULL DEFAULT 15,
    status VARCHAR(32) NOT NULL DEFAULT 'CONFIGURED',
    audio_consent_granted BOOLEAN NOT NULL DEFAULT 0,
    started_at TIMESTAMP,
    ended_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE
);

-- 3. InterviewQuestions Table
CREATE TABLE IF NOT EXISTS InterviewQuestions (
    id VARCHAR(64) PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL,
    question_index INTEGER NOT NULL,
    question_text TEXT NOT NULL,
    category VARCHAR(128) NOT NULL,
    expected_points TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES InterviewSessions(id) ON DELETE CASCADE
);

-- 4. CandidateAnswers Table
-- Spec 12 Note: Do not retain raw audio recordings by default.
-- audio_storage_path is NULL unless user explicitly gave consent.
CREATE TABLE IF NOT EXISTS CandidateAnswers (
    id VARCHAR(64) PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL,
    question_id VARCHAR(64) NOT NULL,
    transcript_text TEXT NOT NULL,
    speech_duration_seconds REAL NOT NULL DEFAULT 0.0,
    is_typed_fallback BOOLEAN NOT NULL DEFAULT 0,
    audio_storage_path VARCHAR(512) DEFAULT NULL,
    recorded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES InterviewSessions(id) ON DELETE CASCADE,
    FOREIGN KEY (question_id) REFERENCES InterviewQuestions(id) ON DELETE CASCADE
);

-- 5. InterviewEvaluations Table
-- Spec 13: Evidence-based feedback linked to candidate's actual answer.
CREATE TABLE IF NOT EXISTS InterviewEvaluations (
    id VARCHAR(64) PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL,
    answer_id VARCHAR(64) NOT NULL,
    relevance_score INTEGER CHECK(relevance_score BETWEEN 1 AND 5),
    technical_accuracy_score INTEGER CHECK(technical_accuracy_score BETWEEN 1 AND 5),
    clarity_score INTEGER CHECK(clarity_score BETWEEN 1 AND 5),
    completeness_score INTEGER CHECK(completeness_score BETWEEN 1 AND 5),
    problem_solving_score INTEGER CHECK(problem_solving_score BETWEEN 1 AND 5),
    confidence_indicators TEXT,
    evidence_feedback TEXT NOT NULL,
    missing_key_concepts TEXT,
    interviewer_ack_reaction TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES InterviewSessions(id) ON DELETE CASCADE,
    FOREIGN KEY (answer_id) REFERENCES CandidateAnswers(id) ON DELETE CASCADE
);

-- 6. InterviewReports Table
-- Spec 13: Transparent rubric breakdown & Score limitations disclaimer.
CREATE TABLE IF NOT EXISTS InterviewReports (
    id VARCHAR(64) PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL UNIQUE,
    overall_score INTEGER CHECK(overall_score BETWEEN 0 AND 100),
    relevance_percentage REAL NOT NULL,
    technical_percentage REAL NOT NULL,
    clarity_percentage REAL NOT NULL,
    completeness_percentage REAL NOT NULL,
    problem_solving_percentage REAL NOT NULL,
    strengths_json TEXT NOT NULL,
    weaknesses_json TEXT NOT NULL,
    actionable_recommendations_json TEXT NOT NULL,
    suggested_practice_topics_json TEXT NOT NULL,
    score_limitations_disclaimer TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (session_id) REFERENCES InterviewSessions(id) ON DELETE CASCADE
);

-- Indices for rapid querying
CREATE INDEX IF NOT EXISTS idx_sessions_user ON InterviewSessions(user_id);
CREATE INDEX IF NOT EXISTS idx_questions_session ON InterviewQuestions(session_id, question_index);
CREATE INDEX IF NOT EXISTS idx_answers_session ON CandidateAnswers(session_id);
CREATE INDEX IF NOT EXISTS idx_evaluations_session ON InterviewEvaluations(session_id);
