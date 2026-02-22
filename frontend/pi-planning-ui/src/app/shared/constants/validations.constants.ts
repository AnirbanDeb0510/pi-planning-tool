export const VALIDATIONS = {
  PAT: {
    REQUIRED: 'Please enter a PAT',
    INVALID: 'Invalid PAT or no permission to access this board',
    ERROR: 'Error validating PAT. Please check your credentials.',
  },
  FEATURE: {
    REQUIRED_ID_AND_PAT: 'Please provide Feature ID and PAT',
  },
  BOARD: {
    MISSING_INFO: 'Board is missing organization or project information',
    MISSING_PREVIEW: 'Missing organization, project, or feature information',
  },
  TEAM_MEMBER: {
    NAME_REQUIRED: 'Team member name cannot be empty',
    NAME_TOO_LONG: 'Team member name must be 100 characters or less',
  },
  CAPACITY: {
    NOT_INTEGER: 'Capacity must be a positive integer',
    NEGATIVE: 'Capacity cannot be negative',
    EXCEEDS: (max: number) => `Capacity cannot exceed sprint duration (${max} working days)`,
  },
  CREATE_BOARD: {
    REQUIRED_FIELDS: 'Please fill in all required fields',
  },
};
