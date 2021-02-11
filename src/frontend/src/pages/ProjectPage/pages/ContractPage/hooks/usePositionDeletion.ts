import { useState, useEffect, useCallback } from 'react';
import { Position, useCurrentContext, useNotificationCenter } from '@equinor/fusion';
import { useContractContext } from '../../../../../contractContex';
import { useAppContext } from '../../../../../appContext';

export default (selectedPositions: Position[]) => {
    const [isDeleting, setIsDeleting] = useState<boolean>(false);
    const [deleteError, setDeleteError] = useState<Error | null>(null);
    const [canDeletePosition, setCanDeletePosition] = useState<boolean>(false);

    const sendNotification = useNotificationCenter();
    const currentContext = useCurrentContext();
    const { contract, dispatchContractAction } = useContractContext();
    const currentProjectId = currentContext?.externalId;
    const currentContractId = contract?.id;
    const { apiClient } = useAppContext();

    const canDeletePositionAsync = async (projectId: string, contractId: string) => {
        const response = await apiClient.canDeleteMppPositionsAsync(projectId, contractId);
        setCanDeletePosition(response);
    };

    useEffect(() => {
        if (currentContractId && currentProjectId) {
            canDeletePositionAsync(currentProjectId, currentContractId);
        }
    }, [currentContractId, currentProjectId]);

    const deletePositionsAsync = useCallback(
        async (projectId: string, contractId: string, positions: Position[]) => {
            setDeleteError(null);
            setIsDeleting(true);
            try {
                const responses = positions.map((position) =>
                    apiClient.deleteMppPositionAsync(projectId, contractId, position.id)
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
                    body: e?.response?.error?.message || '',
                });
            } finally {
                setIsDeleting(false);
            }
        },
        [apiClient]
    );

    const deletePositions = useCallback(async () => {
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

    return { deletePositions, isDeleting, deleteError, canDeletePosition };
};
