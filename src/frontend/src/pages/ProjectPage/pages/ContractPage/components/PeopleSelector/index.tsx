import * as React from 'react';
import * as styles from './styles.less';
import { AccountType } from '../ContractAdminTable';
import PersonnelPicker from './components/PersonnelPicker';
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
    const [selectedPersons, setSelectedPersons] = React.useState<BareBonePerson[]>([]);

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
                <PersonnelPicker onSelect={addPerson} selectedPersons={selectedPersons} />
            </div>
        </div>
    );
};

export default PeopleSelector;
