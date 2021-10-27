
import { SearchableDropdown, SearchableDropdownOption } from '@equinor/fusion-components';
import { FC, useMemo, useCallback } from 'react';
import useAvailableContracts from '../hooks/useAvailableContracts';

type ContractNumberSelectorProps = {
    selectedContractNumber: string | null;
    onSelect: (contractNumber: string) => void;
};

const ContractNumberPicker: FC<ContractNumberSelectorProps> = ({
    selectedContractNumber,
    onSelect,
}) => {
    const { availableContracts } = useAvailableContracts();

    const options = useMemo(() => {
        return availableContracts.map((ac) => ({
            title: ac.contractNumber,
            key: ac.contractNumber,
            isSelected: ac.contractNumber === selectedContractNumber,
        }));
    }, [availableContracts, selectedContractNumber]);

    const onDropdownSelect = useCallback(
        (option: SearchableDropdownOption) => {
            onSelect(option.key);
        },
        [onSelect]
    );

    return (
        <SearchableDropdown id="contract-number-dropdown" label="Contract No." options={options} onSelect={onDropdownSelect} />
    );
};

export default ContractNumberPicker;
