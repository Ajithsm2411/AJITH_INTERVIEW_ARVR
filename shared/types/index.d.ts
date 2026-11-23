export type JobRoleType =
  | 'Software Engineer'
  | 'Frontend Developer'
  | 'Backend Developer'
  | 'Data Analyst'
  | 'Human Resources'
  | 'Custom Role';

export type ExperienceLevelType =
  | 'Fresher (0-1y)'
  | 'Junior (1-3y)'
  | 'Mid-Level (3-6y)'
  | 'Senior (6+y)';

export type InterviewTypeOption =
  | 'Technical Architecture'
  | 'Behavioral (STAR)'
  | 'HR / Culture'
  | 'Mixed Competency';

export type DifficultyLevel = 'Easy' | 'Medium' | 'Hard';

export type DialoguePhaseType =
  | 'IDLE'
  | 'INTERVIEWER_SPEAKING'
  | 'CANDIDATE_RECORDING'
  | 'CANDIDATE_DELIVERING'
  | 'INTERVIEWER_EVALUATING'
  | 'INTERVIEWER_ACKNOWLEDGING'
  | 'SESSION_CONCLUDED';

export type AvatarStateType =
  | 'IDLE'
  | 'SPEAKING'
  | 'LISTENING'
  | 'THINKING'
  | 'WAITING'
  | 'INTERVIEW_COMPLETED';

export interface InterviewSessionConfig {
  jobRole: JobRoleType;
  customRoleTitle?: string;
  experienceLevel: ExperienceLevelType;
  interviewType: InterviewTypeOption;
  difficulty: DifficultyLevel;
  durationMinutes: number;
  audioConsentGranted: boolean;
}

export interface InterviewQuestionItem {
  id: string;
  sessionId: string;
  questionIndex: number;
  questionText: string;
  category: string;
  expectedPoints: string[];
  createdAt: string;
}

export interface CandidateAnswerItem {
  id: string;
  sessionId: string;
  questionId: string;
  transcriptText: string;
  speechDurationSeconds: number;
  isTypedFallback: boolean;
  audioStoragePath?: string | null;
  recordedAt: string;
}

export interface EvaluationItem {
  id: string;
  sessionId: string;
  answerId: string;
  relevanceScore: number;
  technicalAccuracyScore: number;
  clarityScore: number;
  completenessScore: number;
  problemSolvingScore: number;
  confidenceIndicators: string;
  evidenceFeedback: string;
  missingKeyConcepts: string[];
  interviewerAckReaction: string;
  createdAt: string;
}

export interface InterviewReportItem {
  id: string;
  sessionId: string;
  overallScore: number;
  relevancePercentage: number;
  technicalPercentage: number;
  clarityPercentage: number;
  completenessPercentage: number;
  problemSolvingPercentage: number;
  strengths: string[];
  weaknesses: string[];
  actionableRecommendations: string[];
  suggestedPracticeTopics: string[];
  qnaReview: {
    questionIndex: number;
    category: string;
    questionText: string;
    candidateTranscript: string;
    candidateSpeechDuration: number;
    score: number;
    evidenceFeedback: string;
  }[];
  scoreLimitationsDisclaimer: string;
  createdAt: string;
}
