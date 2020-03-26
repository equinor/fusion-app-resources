import * as React from 'react';
import {
    useApiClients,
    RoleDescription,
    Position,
    useCurrentContext,
} from '@equinor/fusion';
import { MarkdownViewer, Spinner, ErrorMessage } from '@equinor/fusion-components';
import PersonalTaskDescription from './PersonalTaskDescription';

type RoleDescriptionTabProps = {
    selectedPosition: Position;
    onEditChange: (edit: boolean) => void;
    filterToDate: Date
};

const RoleDescriptionTab: React.FC<RoleDescriptionTabProps> = ({
    onEditChange,
    selectedPosition,
    filterToDate
}) => {
    const apiClients = useApiClients();
    const project = useCurrentContext();
    const [isFetchingDescription, setIsFetchingDescription] = React.useState<boolean>(false);
    const [roleDescription, setRoleDescription] = React.useState<RoleDescription | null>(null);
    const [error, setError] = React.useState<string | null>(null);

    const getRoleDescriptionAsync = async (positionId: string, projectId: string) => {
        setIsFetchingDescription(true);
        setError(null);

        try {
            const response = await apiClients.org.getRoleDescriptionV2Async(projectId, positionId);
            setRoleDescription(response.data);
            setIsFetchingDescription(false);
        } catch {
            setIsFetchingDescription(false);
            setRoleDescription(null);
            setError('Could not fetch position role descriptions');
        }
    };

    React.useEffect(() => {
        if (project?.externalId) {
            getRoleDescriptionAsync(selectedPosition.id, project.externalId);
        }
    }, [selectedPosition, project]);

    if (isFetchingDescription) {
        return <Spinner centered />;
    }

    if (error) {
        return <ErrorMessage hasError message={error} />;
    }
    if (!roleDescription) {
        return null;
    }
    return (
        <>
            <h2>Generic Role Description</h2>

            <MarkdownViewer
                markdown={
                    roleDescription.generic.content
                        ? roleDescription.generic.content
                        : 'No role description'
                }
            />
            <h2>Personal Task Description</h2>
            {selectedPosition.instances.map(instance => (
                <PersonalTaskDescription
                    selectedInstance={instance}
                    roleDescription={roleDescription}
                    onEditChange={onEditChange}
                    filterToDate={filterToDate}
                    selectedPosition={selectedPosition}
                />
            ))}
        </>
    );
};

export default RoleDescriptionTab;
