import * as React from 'react';
import { SearchableDropdown, SearchableDropdownOption } from '@equinor/fusion-components';
import useAvailableContracts from '../hooks/useAvailableContracts';

type ContractNumberSelectorProps = {
    selectedContractNumber: string | null;
    onSelect: (contractNumber: string) => void;
};

const ContractNumberSelector: React.FC<ContractNumberSelectorProps> = ({
    selectedContractNumber,
    onSelect,
}) => {
    const { availableContracts } = useAvailableContracts();

    const options = React.useMemo(() => {
        return availableContracts.map(ac => ({
            title: ac.contractNumber,
            key: ac.contractNumber,
            isSelected: ac.contractNumber === selectedContractNumber,
        }));
    }, [availableContracts, selectedContractNumber]);

    const onDropdownSelect = React.useCallback(
        (option: SearchableDropdownOption) => {
            onSelect(option.key);
        },
        [onSelect]
    );

    return (
        <SearchableDropdown label="Contract Nr." options={options} onSelect={onDropdownSelect} />
    );
};

export default ContractNumberSelector;
