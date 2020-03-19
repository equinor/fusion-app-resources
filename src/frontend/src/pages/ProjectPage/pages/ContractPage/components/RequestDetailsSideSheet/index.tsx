import * as React from 'react';
import PersonnelRequest from '../../../../../../models/PersonnelRequest';
import { ModalSideSheet, Tabs, Tab, Accordion, AccordionItem } from '@equinor/fusion-components';
import RequestDetails from './RequestDetails';
import useCurrentRequest from './hooks/useCurrentRequest';
import DetailedRequestWorkflow from '../DetailedRequestWorkflow';
import * as styles from './styles.less';

type RequestDetailsSideSheetProps = {
    requests: PersonnelRequest[] | null;
};
type AccordionOpenDictionary = {
    [id: string]: boolean;
};

const RequestDetailsSideSheet: React.FC<RequestDetailsSideSheetProps> = ({ requests }) => {
    const { currentRequest, setCurrentRequest } = useCurrentRequest(requests);
    const [activeTabKey, setActiveTabKey] = React.useState<string>('general');
    const [openAccordions, setOpenAccordions] = React.useState<AccordionOpenDictionary>({});

    const showSideSheet = React.useMemo(() => currentRequest !== null, [currentRequest]);

    const onClose = React.useCallback(() => {
        setCurrentRequest(null);
    }, [setCurrentRequest]);

    const handleAccordionStateChange = (id: string) => {
        setOpenAccordions({ ...openAccordions, [id]: !openAccordions[id] });
    };

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
                        <DetailedRequestWorkflow workflow={currentRequest.workflow} />
                        <Accordion>
                            <AccordionItem
                                label="Description"
                                onChange={() => handleAccordionStateChange('description')}
                                key="description"
                                isOpen={openAccordions['description']}
                            >
                                <div>test</div>
                            </AccordionItem>
                            <AccordionItem
                                label="Person"
                                onChange={() => handleAccordionStateChange('person')}
                                key="person"
                                isOpen={openAccordions['person']}
                            >
                                <div>test</div>
                            </AccordionItem>
                            <AccordionItem
                                label="Comments"
                                onChange={() => handleAccordionStateChange('comments')}
                                key="comments"
                                isOpen={openAccordions['comments']}
                            >
                                <div>test</div>
                            </AccordionItem>
                        </Accordion>
                    </div>
                </Tab>
                <Tab tabKey="description" title="Description">
                    <div className={styles.tabContainer}>
                        <RequestDetails request={currentRequest} />
                    </div>
                </Tab>
            </Tabs>
        </ModalSideSheet>
    );
};
export default RequestDetailsSideSheet;
