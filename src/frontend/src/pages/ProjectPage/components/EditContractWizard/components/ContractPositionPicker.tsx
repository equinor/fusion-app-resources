import * as React from "react";
import { PositionPicker } from '@equinor/fusion-components';
import { useCurrentContext, useApiClients, Position } from '@equinor/fusion';
import { FC } from "react";

type ContractPositionPickerProps = {
    label: string;
    selectedPosition: Position | null;
    onSelect: (position: Position) => void;
    contractId?: string;
};

const ContractPositionPicker: FC<ContractPositionPickerProps> = ({ label, selectedPosition, onSelect, contractId }) => {
    const currentOrgProject = useCurrentContext();

    if (!currentOrgProject || !currentOrgProject.externalId) {
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