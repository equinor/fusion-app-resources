import * as React from 'react';
import * as styles from './styles.less';
import {
    EditIcon,
    IconButton,
    CloseIcon,
    SaveIcon,
    PersonCard,
    TextArea,
    MarkdownViewer,
    Spinner,
} from '@equinor/fusion-components';
import {
    PositionInstance,
    useApiClients,
    useNotificationCenter,
    RoleDescription,
    formatDate,
    useCurrentContext,
    Position,
} from '@equinor/fusion';
import { getInstances } from '../../../../../../orgHelpers';

type PersonalTaskDescriptionProps = {
    selectedInstance: PositionInstance;
    roleDescription: RoleDescription;
    selectedPosition: Position;
    onEditChange: (edit: boolean) => void;
    filterToDate: Date;
};

const PersonalTaskDescription: React.FC<PersonalTaskDescriptionProps> = ({
    selectedInstance,
    roleDescription,
    onEditChange,
    selectedPosition,
    filterToDate,
}) => {
    const [isEditing, setIsEditing] = React.useState<boolean>(false);
    const [inputValue, setInputValue] = React.useState<string>('');
    const [isSaving, setIsSaving] = React.useState<boolean>(false);
    const [canEditTaskDescription, setCanEditTaskDescription] = React.useState<boolean>(false);
    const [lastUpdated, setLastUpdated] = React.useState<Date | null>(null);
    const [error, setError] = React.useState<string | null>(null);

    const apiClients = useApiClients();
    const sendNotification = useNotificationCenter();
    const project = useCurrentContext();

    const canEditAsync = async (azureUniqueId: string, projectId: string) => {
        const response = await apiClients.org.canEditPersonalTaskDescriptionAsync(
            projectId,
            azureUniqueId
        );
        setCanEditTaskDescription(response);
    };

    const saveTaskDescriptionAsync = async (azureUniqueId: string, projectId: string) => {
        setIsSaving(true);

        try {
            const response = await apiClients.org.updatePersonalTaskDescriptionAsync(
                projectId,
                azureUniqueId,
                inputValue
            );
            setInputValue(response.data);
            setIsEditing(false);
            setIsSaving(false);
            setLastUpdated(new Date());
        } catch {
            setIsSaving(false);
            const notificationResponse = await sendNotification({
                level: 'high',
                title: "Can't save personal task description",
                confirmLabel: 'Try again',
                cancelLabel: 'Cancel',
            });
            if (notificationResponse.confirmed) {
                saveTaskDescriptionAsync(azureUniqueId, projectId);
            }
            return;
        }
    };

    React.useEffect(() => {
        if (selectedInstance.assignedPerson?.azureUniqueId && project?.externalId) {
            canEditAsync(selectedInstance.assignedPerson.azureUniqueId, project.externalId);
        } else {
            setCanEditTaskDescription(false);
        }
    }, [selectedInstance, project]);

    React.useEffect(() => {
        onEditChange(isEditing);
    }, [isEditing]);

    const currentRoleDescription = React.useMemo(
        () =>
            selectedPosition &&
            roleDescription.persons.find(person =>
                getInstances(selectedPosition, filterToDate).some(
                    i => i.assignedPerson?.azureUniqueId === person.person.azureUniqueId
                )
            ),
        [selectedPosition, roleDescription]
    );

    React.useEffect(() => {
        if (!currentRoleDescription) {
            return;
        }
        if (currentRoleDescription.content) {
            setInputValue(currentRoleDescription.content);
        }
        if (currentRoleDescription.lastUpdated) {
            setLastUpdated(currentRoleDescription.lastUpdated);
        }
    }, [currentRoleDescription]);

    React.useEffect(() => {
        if (inputValue.length >= 1000) {
            setError('Maximum characters exceeded');
        } else {
            if (error) {
                setError(null);
            }
        }
    }, [inputValue]);

    const iconBar = React.useMemo(() => {
        if (!isEditing) {
            return (
                <IconButton onClick={() => setIsEditing(true)} disabled={!canEditTaskDescription}>
                    <EditIcon />
                </IconButton>
            );
        }
        return (
            <>
                <IconButton
                    onClick={() => {
                        setInputValue(currentRoleDescription?.content || '');
                        setIsEditing(false);
                    }}
                >
                    <CloseIcon />
                </IconButton>
                <IconButton
                    onClick={() =>
                        canEditTaskDescription &&
                        selectedInstance.assignedPerson &&
                        selectedInstance.assignedPerson.mail &&
                        !error &&
                        project?.externalId &&
                        saveTaskDescriptionAsync(
                            selectedInstance.assignedPerson.azureUniqueId,
                            project.externalId
                        )
                    }
                    disabled={!canEditTaskDescription || !!error}
                >
                    {isSaving ? <Spinner inline /> : <SaveIcon />}
                </IconButton>
            </>
        );
    }, [
        isEditing,
        selectedInstance,
        isSaving,
        canEditTaskDescription,
        inputValue,
        currentRoleDescription,
        error,
    ]);

    if (!selectedInstance.assignedPerson || !selectedInstance.assignedPerson.azureUniqueId) {
        return (
            <>
                <p>No description</p>
            </>
        );
    }
    return (
        <div className={styles.personalTaskDescription}>
            <div className={styles.container}>
                <div className={styles.descriptionBar}>
                    <PersonCard
                        person={selectedInstance.assignedPerson || undefined}
                        showJobTitle
                    />
                    <div>{iconBar}</div>
                </div>
                <div className={styles.titleBar}>
                    <span>
                        Personal task description {isEditing && `${inputValue.length}/1000`}
                    </span>
                    <span>
                        Last updated:
                        {lastUpdated ? formatDate(lastUpdated) : 'N/A'}
                    </span>
                </div>

                <div className={styles.textInput}>
                    {!isEditing ? (
                        <div className={styles.viewMarkdown}>
                            <MarkdownViewer markdown={inputValue} />
                        </div>
                    ) : (
                        <>
                            <TextArea
                                onChange={value => value.length <= 10000 && setInputValue(value)}
                                value={inputValue}
                                label="Edit personal task description"
                                disabled={!isEditing}
                                error={!!error}
                                errorMessage={error || undefined}
                            />
                            <div className={styles.inputValueCounter}></div>
                        </>
                    )}
                </div>
            </div>
        </div>
    );
};

export default PersonalTaskDescription;
