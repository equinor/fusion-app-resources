import * as React from "react";
import { PositionPicker } from '@equinor/fusion-components';
import { useCurrentContext, useApiClients, Position } from '@equinor/fusion';

type ContractPositionPickerProps = {
    label: string;
    selectedPosition: Position | null;
    onSelect: (position: Position) => void;
    contractId?: string;
};

const ContractPositionPicker: React.FC<ContractPositionPickerProps> = ({ label, selectedPosition, onSelect, contractId }) => {
    const currentContext = useCurrentContext();
    const currentOrgProject = currentContext as any;

    if (!currentOrgProject) {
        return null;
    }

    return (
        <PositionPicker
            label={label}
            selectedPosition={selectedPosition}
            projectId={currentOrgProject.externalId}
            contractId={contractId}
            onSelect={onSelect}
        />
    );
};

export default ContractPositionPicker;