import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import {
    ModalSideSheet,
    Tabs,
    Tab,
    Accordion,
    AccordionItem,
    ErrorMessage,
} from '@equinor/fusion-components';
import RequestDetails from './RequestDetails';
import useCurrentRequest from './hooks/useCurrentRequest';
import RequestWorkflow from '../RequestWorkflow';
import * as styles from './styles.less';
import CompactPersonDetails from './CompactPersonDetails';

type RequestDetailsSideSheetProps = {
    requests: PersonnelRequest[] | null;
};
type AccordionOpenDictionary = {
    description: boolean;
    person: boolean;
    comments: boolean;
};

const RequestDetailsSideSheet: React.FC<RequestDetailsSideSheetProps> = ({ requests }) => {
    const { currentRequest, setCurrentRequest } = useCurrentRequest(requests);
    const [activeTabKey, setActiveTabKey] = React.useState<string>('general');
    const [openAccordions, setOpenAccordions] = React.useState<AccordionOpenDictionary>({
        comments: true,
        description: true,
        person: true,
    });

    const showSideSheet = React.useMemo(() => currentRequest !== null, [currentRequest]);

    const onClose = React.useCallback(() => {
        setCurrentRequest(null);
    }, [setCurrentRequest]);

    const handleAccordionStateChange = React.useCallback(
        (id: keyof AccordionOpenDictionary) => {
            setOpenAccordions({ ...openAccordions, [id]: !openAccordions[id] });
        },
        [setOpenAccordions, openAccordions]
    );

    if (!currentRequest) {
        return null;
    }

    return (
        <ModalSideSheet
            show={showSideSheet}
            header={currentRequest.position?.basePosition?.name || ''}
            onClose={onClose}
        >
            <Tabs activeTabKey={activeTabKey} onChange={setActiveTabKey}>
                <Tab tabKey="general" title="General">
                    <div className={styles.tabContainer}>
                        <div className={styles.container}>
                            <RequestWorkflow workflow={currentRequest.workflow} />
                        </div>
                        <div className={styles.separator} />
                        <div className={styles.container}>
                            <Accordion>
                                <AccordionItem
                                    label="Description"
                                    onChange={() => handleAccordionStateChange('description')}
                                    key="description"
                                    isOpen={openAccordions.description}
                                >
                                    <RequestDetails request={currentRequest} />
                                </AccordionItem>
                                <AccordionItem
                                    label="Person"
                                    onChange={() => handleAccordionStateChange('person')}
                                    key="person"
                                    isOpen={openAccordions.person}
                                >
                                    {currentRequest.person ? (
                                        <CompactPersonDetails personnel={currentRequest.person} />
                                    ) : (
                                        <ErrorMessage
                                            hasError
                                            errorType="noData"
                                            message="Could not find a person for this request"
                                        />
                                    )}
                                </AccordionItem>
                                <AccordionItem
                                    label="Comments"
                                    onChange={() => handleAccordionStateChange('comments')}
                                    key="comments"
                                    isOpen={openAccordions.comments}
                                >
                                    <div>test</div>
                                </AccordionItem>
                            </Accordion>
                        </div>
                    </div>
                </Tab>
                <Tab tabKey="description" title="Description">
                    <div className={styles.tabContainer}>
                        <div className={styles.container}>
                            <RequestDetails request={currentRequest} />
                        </div>
                    </div>
                </Tab>
            </Tabs>
        </ModalSideSheet>
    );
};
export default RequestDetailsSideSheet;
