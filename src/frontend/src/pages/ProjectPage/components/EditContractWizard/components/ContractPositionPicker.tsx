import * as React from "react";
import { PositionPicker } from '@equinor/fusion-components';
import { useCurrentContext, useApiClients, Position } from '@equinor/fusion';

type ContractPositionPickerProps = {
    label: string;
    selectedPositionId: string | null;
    onSelect: (positionId: string) => void;
    contractId?: string;
};

const ContractPositionPicker: React.FC<ContractPositionPickerProps> = ({ label, selectedPositionId, onSelect, contractId }) => {
    const currentContext = useCurrentContext();
    const currentOrgProject = currentContext as any;

    const [
        selectedPosition,
        setSelectedPosition,
    ] = React.useState<Position | null>(null);
    const apiClients = useApiClients();

    React.useEffect(() => {
        if (
            currentOrgProject &&
            selectedPositionId &&
            (!selectedPosition ||
                selectedPosition.id !== selectedPositionId)
        ) {
            apiClients.org
                .getPositionAsync(
                    currentOrgProject.externalId,
                    selectedPositionId
                )
                .then(p => setSelectedPosition(p.data));
        }
    }, [selectedPositionId]);

    if (!currentOrgProject) {
        return null;
    }

    return (
        <PositionPicker
            label={label}
            selectedPosition={selectedPosition}
            projectId={currentOrgProject.externalId}
            contractId={contractId}
            onSelect={position => {
                setSelectedPosition(position);
                onSelect(position.id);
            }}
        />
    );
};

export default ContractPositionPicker;