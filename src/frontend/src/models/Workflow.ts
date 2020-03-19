import Person from './Person';

export type WorkflowState =
    | 'Running'
    | 'Canceled'
    | 'Error'
    | 'Completed'
    | 'Terminated'
    | 'Unknown';

export type WorkflowStepState = 'Pending' | 'Approved' | 'Rejected' | 'Skipped' | 'unknown';

export type WorkflowStep = {
    id: string;
    name: string;
    isCompleted: boolean;
    state:WorkflowStepState
    started: Date | null;
    completed: Date | null;
    dueDate: Date | null;
    completedBy: Person | null;
    description: string;
    reason: string | null;
    previousStep: string;
    nextStep: string;

};

type Workflow = {
    logicAppName: string | null;
    logicAppVersion: string | null;
    state: WorkflowState;
    steps: WorkflowStep[];
};

export default Workflow;
