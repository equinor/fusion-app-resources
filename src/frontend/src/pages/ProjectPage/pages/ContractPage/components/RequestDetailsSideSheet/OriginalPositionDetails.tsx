import * as React from 'react';
import PersonnelRequestPosition from '../../../../../../models/PersonnelRequestPosition';
import Personnel from '../../../../../../models/Personnel';
import PersonPositionsDetails from '../PersonPositionsDetails';
import * as styles from './styles.less';
import classNames from 'classnames';
import PositionIdCard from './PositionIdCard';
import { formatDate } from '@equinor/fusion';
import CompactPersonDetails from './CompactPersonDetails';
import usePersonnel from '../../pages/ManagePersonnelPage/hooks/usePersonnel';
import { Accordion, AccordionItem } from '@equinor/fusion-components';

type OriginalPositionDetailsProps = {
    position: PersonnelRequestPosition | null;
    person: Personnel | null;
};

type AccordionOpenDictionary = {
    description: boolean;
    person: boolean;
};

type ItemFieldProps = {
    fieldName: string;
    title: string;
};

const ItemField: React.FC<ItemFieldProps> = ({ fieldName, title, children }) => (
    <div className={classNames(styles.textField, styles[fieldName])}>
        <span className={styles.title}>{title}</span>
        <span className={styles.content}>{children}</span>
    </div>
);

const OriginalPositionetails: React.FC<OriginalPositionDetailsProps> = ({ position, person }) => {
    const { personnel, isFetchingPersonnel, personnelError } = usePersonnel();

    const [openAccordions, setOpenAccordions] = React.useState<AccordionOpenDictionary>({
        description: true,
        person: true,
    });

    const handleAccordionStateChange = React.useCallback(
        (id: keyof AccordionOpenDictionary) => {
            setOpenAccordions({ ...openAccordions, [id]: !openAccordions[id] });
        },
        [setOpenAccordions, openAccordions]
    );

    if (!position || !person) {
        return null;
    }

    const originalPerson = personnel.find(p => p.mail === person.mail) || null;

    return (
        <div>
            <Accordion>
                <AccordionItem
                    label="Description"
                    onChange={() => handleAccordionStateChange('description')}
                    key="description"
                    isOpen={openAccordions.description}
                >
                    <div className={styles.requestDetails}>
                        <ItemField fieldName="basePosition" title="Base position">
                            {position?.basePosition?.name || 'N/A'}
                        </ItemField>
                        <ItemField fieldName="customPosition" title="Custom position title">
                            {position?.name || 'N/A'}
                        </ItemField>
                        <ItemField fieldName="taskOwner" title="Task owner">
                            <PositionIdCard
                                positionId={position?.taskOwner?.positionId || undefined}
                            />
                        </ItemField>
                        <ItemField fieldName="fromDate" title="From date">
                            {position?.appliesFrom ? formatDate(position.appliesFrom) : 'N/A'}
                        </ItemField>
                        <ItemField fieldName="toDate" title="To date">
                            {position?.appliesTo ? formatDate(position.appliesTo) : 'N/A'}
                        </ItemField>
                        <ItemField fieldName="workload" title="Workload">
                            {position?.workload.toString() + '%' || 'N/A'}
                        </ItemField>
                    </div>
                </AccordionItem>
                <AccordionItem
                    label="Person"
                    onChange={() => handleAccordionStateChange('person')}
                    key="person"
                    isOpen={openAccordions.person}
                >
                    {originalPerson && <CompactPersonDetails personnel={originalPerson} />}
                </AccordionItem>
            </Accordion>
        </div>
    );
};

export default OriginalPositionetails;
