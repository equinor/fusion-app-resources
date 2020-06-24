import * as React from 'react';
import * as styles from './styles.less';
import { AccountType } from '../ContractAdminTable';
import ExternalPicker from './components/ExternalPicker';
import RemovablePersonDetails from './components/RemovablePersonDetails';

type PeopleSelectorProps = {
    accountType: AccountType;
};

export type BareBonePerson = {
    azureUniqueId: string;
    name: string;
    mail: string | null;
};

const PeopleSelector: React.FC<PeopleSelectorProps> = ({ accountType }) => {
    const [selectedPersons, setSelectedPersons] = React.useState<BareBonePerson[]>([
        {
            azureUniqueId: '7ecc8cd2-077b-4a4a-953f-8e02c0f07c24',
            mail: 'eslsa@equinor.com',
            name: 'Eskil Sand',
        },
        {
            azureUniqueId: '7ecc8cd2-077b-4a4a-953f-8e02c0f07c24',
            mail: 'eslsa@equinor.com',
            name: 'Eskil Sand',
        },
    ]);

    const removePerson = React.useCallback((person: BareBonePerson) => {
        setSelectedPersons((persons) =>
            persons.filter((p) => p.azureUniqueId !== person.azureUniqueId)
        );
    }, []);

    const addPerson = React.useCallback((person: BareBonePerson) => {
        setSelectedPersons((persons) => [...persons, person]);
    }, []);

    return (
        <div className={styles.container}>
            {selectedPersons.map((person) => (
                <RemovablePersonDetails person={person} onRemove={removePerson} />
            ))}
            <div className={styles.personPicker}>
                {accountType === 'external' ? (
                    <ExternalPicker onSelect={addPerson} selectedPersons={selectedPersons} />
                ) : null}
            </div>
        </div>
    );
};

export default PeopleSelector;
