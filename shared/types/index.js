/**
 * VR Telepresence AI Interview Platform — Shared Type Definitions & Schemas
 */

export const JobRole = Object.freeze({
  SOFTWARE_ENGINEER: 'Software Engineer',
  FRONTEND_DEVELOPER: 'Frontend Developer',
  BACKEND_DEVELOPER: 'Backend Developer',
  DATA_ANALYST: 'Data Analyst',
  HUMAN_RESOURCES: 'Human Resources',
  CUSTOM_ROLE: 'Custom Role'
});

export const ExperienceLevel = Object.freeze({
  FRESHER: 'Fresher (0-1y)',
  JUNIOR: 'Junior (1-3y)',
  MID_LEVEL: 'Mid-Level (3-6y)',
  SENIOR: 'Senior (6+y)'
});

export const InterviewType = Object.freeze({
  TECHNICAL: 'Technical Architecture',
  BEHAVIORAL: 'Behavioral (STAR)',
  HR: 'HR / Culture',
  MIXED: 'Mixed Competency'
});

export const Difficulty = Object.freeze({
  EASY: 'Easy',
  MEDIUM: 'Medium',
  HARD: 'Hard'
});

export const DurationOption = Object.freeze({
  FIVE_MINUTES: 5,
  TEN_MINUTES: 10,
  FIFTEEN_MINUTES: 15,
  THIRTY_MINUTES: 30
});

export const SessionStatus = Object.freeze({
  CONFIGURED: 'CONFIGURED',
  ACTIVE: 'ACTIVE',
  PAUSED: 'PAUSED',
  COMPLETED: 'COMPLETED',
  CANCELLED: 'CANCELLED'
});

export const DialoguePhase = Object.freeze({
  IDLE: 'IDLE',
  INTERVIEWER_SPEAKING: 'INTERVIEWER_SPEAKING',
  CANDIDATE_RECORDING: 'CANDIDATE_RECORDING',
  CANDIDATE_DELIVERING: 'CANDIDATE_DELIVERING',
  INTERVIEWER_EVALUATING: 'INTERVIEWER_EVALUATING',
  INTERVIEWER_ACKNOWLEDGING: 'INTERVIEWER_ACKNOWLEDGING',
  SESSION_CONCLUDED: 'SESSION_CONCLUDED'
});

export const AvatarState = Object.freeze({
  IDLE: 'IDLE',
  SPEAKING: 'SPEAKING',
  LISTENING: 'LISTENING',
  THINKING: 'THINKING',
  WAITING: 'WAITING',
  INTERVIEW_COMPLETED: 'INTERVIEW_COMPLETED'
});

export const EvaluationCriteria = Object.freeze({
  RELEVANCE: 'Relevance to Question',
  TECHNICAL_ACCURACY: 'Technical Accuracy',
  COMMUNICATION_CLARITY: 'Communication Clarity',
  COMPLETENESS: 'Completeness',
  PROBLEM_SOLVING: 'Problem-Solving Approach',
  CONFIDENCE_INDICATORS: 'Confidence & Articulation (Non-Acoustic)'
});

export const ScoreLimitationsDisclaimer = Object.freeze({
  TEXT: 'Notice: AI-generated evaluations, rubrics, and scores are automated simulated estimates designed for interview practice, reflection, and self-improvement. They do not constitute official hiring decisions, psychometric determinations, or definitive employability evaluations.'
});
