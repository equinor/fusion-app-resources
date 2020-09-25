import * as React from 'react';
import { Position, useApiClients, useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useContractContext } from '../../../../../contractContex';

export default (selectedPositions: Position[]) => {
    const [isDeleting, setIsDeleting] = React.useState<boolean>(false);
    const [deleteError, setDeleteError] = React.useState<Error | null>(null);
    const sendNotification = useNotificationCenter();
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const currentProjectId = currentContext?.externalId;
    const currentContractId = contract?.id;
    const apiClients = useApiClients();

    const deletePositionsAsync = React.useCallback(
        async (projectId: string, contractId: string, positions: Position[]) => {
            setDeleteError(null);
            setIsDeleting(true);
            try {
                const responses = positions.map((position) =>
                    apiClients.org.deleteContractPositionAsync(projectId, contractId, position.id)
                );
                await Promise.all(responses);

                dispatchContractAction({
                    verb: 'delete',
                    collection: 'actualMpp',
                    payload: positions,
                });

                sendNotification({
                    level: 'low',
                    title: ` ${positions.map((p) => p.name || '').join(', ')} has been deleted`,
                });
            } catch (e) {
                setDeleteError(e);
                sendNotification({
                    level: 'high',
                    title: 'Failed to delete positions(s)',
                });
            } finally {
                setIsDeleting(false);
            }
        },
        [apiClients]
    );

    const deletePositions = React.useCallback(async () => {
        if (!currentProjectId || !currentContractId || selectedPositions.length <= 0) {
            return;
        }
        const userApproval = await sendNotification({
            level: 'high',
            title: `Are you sure you want to delete ${selectedPositions.length} positions?`,
        });
        if (userApproval.confirmed) {
            await deletePositionsAsync(currentProjectId, currentContractId, selectedPositions);
        }
    }, [currentProjectId, currentContractId, selectedPositions]);

    return { deletePositions, isDeleting, deleteError };
};
