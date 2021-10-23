using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Campaign
{
    class Program
    {
        public enum CommandCodes
        {
            CreateProduct = 1,
            GetProductInfo = 2,
            CreateOrder = 3,
            CreateCampaign = 4,
            GetCampaignInfo = 5,
            IncreaseTime = 6
        }

        #region common variables

        static List<Product> productList = new List<Product>();
        static List<Order> orderList = new List<Order>();
        static List<Campaign> campaignList = new List<Campaign>();

        static int systemTimeDuration = 0;
        static double fixedDiscountRate = 0;
        static double[,] timeBasedDiscountRates = new double[3, 2]{
                                                                      {0.25, 0},
                                                                      {0.50, 0},
                                                                      {0.75, 0}
                                                                  };

        #endregion

        static void Main(string[] args)
        {
            GetCommandList();

            do
            {
                Console.WriteLine();
                Console.Write("Please enter a command number between 1 to 6:");

                int commandNumber = 0;
                do
                {
                    Int32.TryParse(Console.ReadLine(), out commandNumber);
                    if (commandNumber > 6 || commandNumber < 1)
                        Console.Write("Please enter a command number between 1 to 6:");

                } while (commandNumber > 6 || commandNumber < 1);

                Console.WriteLine();

                switch (commandNumber)
                {
                    case (int)CommandCodes.CreateProduct:
                        CreateProduct();

                        break;
                    case (int)CommandCodes.GetProductInfo:
                        Console.Write("Please enter product code:");
                        string productCode = Console.ReadLine();
                        GetProductInfo(productCode);

                        break;
                    case (int)CommandCodes.CreateOrder:
                        CreateOrder();

                        break;
                    case (int)CommandCodes.CreateCampaign:
                        CreateCampaign();

                        break;
                    case (int)CommandCodes.GetCampaignInfo:
                        Console.Write("Please enter campaign code:");
                        string campaignCode = Console.ReadLine();
                        GetCampaignInfo(campaignCode);

                        break;
                    case (int)CommandCodes.IncreaseTime:
                        IncreaseTime();

                        break;
                    default:
                        break;
                }

            } while (true);

        }

        /// <summary>
        /// Gets the command list
        /// </summary>
        public static void GetCommandList()
        {
            Console.WriteLine("COMMAND LIST");
            Console.WriteLine("------------");
            Console.WriteLine("1.create_product");
            Console.WriteLine("2.get_product_info");
            Console.WriteLine("3.create_order");
            Console.WriteLine("4.create_campaign");
            Console.WriteLine("5.get_campaign_info");
            Console.WriteLine("6.increase_time");
        }

        /// <summary>
        /// Product creation process
        /// </summary>
        public static void CreateProduct()
        {
            Console.WriteLine("Please enter Product info:");

            Console.Write("Product Code:");
            string productCode = Console.ReadLine();
            while (productList.Any(c => c.code == productCode))
            {
                Console.Write("Product with code {0} already exists. Please enter a new Product Code:", productCode);
                productCode = Console.ReadLine();
            }

            Console.Write("Price:");
            double price = 0;
            while(!Double.TryParse(Console.ReadLine(), out price))
            {
                Console.Write("Please enter a price value:");
            };

            Console.Write("Stock:");
            int stock = 0;
            while (!Int32.TryParse(Console.ReadLine(), out stock))
            {
                Console.Write("Please enter a stok value:");
            };

            Product product = new Product();
            product.code = productCode;
            product.price = price;
            product.stock = stock;
            productList.Add(product);

            CommonOutput(string.Format("Product created; code {0}, price {1}, stock {2}", productCode, price, stock));
        }

        /// <summary>
        /// Fetch product information
        /// </summary>
        /// <param name="productCode">Product code</param>
        public static void GetProductInfo(string productCode)
        {
            var product = productList.Where(p => p.code == productCode).FirstOrDefault();
            if(product != null)
            {
                double price = GetProductPriceWithDemand(product.code);
                int productOrderedCount = orderList.Where(o => o.productCode == productCode).Sum(c => c.quantity);
                CommonOutput(string.Format("Product {0} info; price {1}, stock {2}", product.code, price, product.stock - productOrderedCount));
            }
            else
            {
                CommonOutput(string.Format("Product does not exists with code:{0}", productCode));
            }
        }

        /// <summary>
        /// Order creation process
        /// </summary>
        public static void CreateOrder()
        {
            Console.WriteLine("Please enter Order info:");

            Console.Write("Product Code:");
            string productCode = Console.ReadLine();
            while (!productList.Any(c => c.code == productCode))
            {
                Console.Write("Product with code {0} does not exists in Products. Please enter an exists Product Code:", productCode);
                productCode = Console.ReadLine();
            }

            int productStock = productList.Where(p => p.code == productCode).Sum(p => p.stock);
            int productOrderedCount = orderList.Where(o => o.productCode == productCode).Sum(c => c.quantity);
            int remainingProductAmount = productStock - productOrderedCount;

            int quantity = 0;
            Console.Write("Quantity:");
            do
            {
                while (!Int32.TryParse(Console.ReadLine(), out quantity))
                {
                    Console.Write("Please enter a Quantity value:");
                };

                if (remainingProductAmount < quantity)
                    Console.Write("The remaining amount of the product is not enough. Max limit is {0}. Please enter new Quantity:", remainingProductAmount);

            } while (remainingProductAmount < quantity);

            Order order = new Order();
            order.productCode = productCode;
            order.price = GetProductPriceWithDemand(productCode);
            order.quantity = quantity;
            order.createDate = DateTime.Now;
            order.CampaignName = campaignList.Where(c => c.productCode == productCode && c.duration > systemTimeDuration).Select(c => c.name).FirstOrDefault();
            orderList.Add(order);

            CommonOutput(string.Format("Order created; product {0}, quantity {1}", productCode, quantity));
        }

        /// <summary>
        /// Calculate product price according to demand
        /// </summary>
        /// <param name="productCode"></param>
        /// <returns></returns>
        public static double GetProductPriceWithDemand(string productCode)
        {
            var product = productList.Where(p => p.code == productCode).FirstOrDefault();
            double price = product.price;

            var productCampaign = campaignList.Where(c => c.productCode == product.code && c.duration > systemTimeDuration).FirstOrDefault();
            if(productCampaign != null)
            {
                int campaignSalesCount = orderList.Where(o => o.productCode == productCampaign.productCode && o.createDate > productCampaign.createDate).Sum(o => o.quantity);

                //if the campaign did not reach its sales target
                if (productCampaign.targetSalesCount > campaignSalesCount)
                {
                    #region Price calculation according to some periods during the campaign

                    FixDiscountRatesByManipulationLimit(productCampaign.priceManipulationLimit);

                    if (productCampaign.duration * timeBasedDiscountRates[2, 0] < systemTimeDuration)
                    {
                        if (campaignSalesCount < productCampaign.targetSalesCount * timeBasedDiscountRates[2, 0])
                        {
                            price = product.price - (product.price * productCampaign.priceManipulationLimit / 100);
                        }
                        else if (campaignSalesCount == productCampaign.targetSalesCount * timeBasedDiscountRates[2, 0])
                        {
                            price = product.price - (product.price * fixedDiscountRate);
                        }
                        else
                        {
                            price = product.price - (product.price * timeBasedDiscountRates[2, 1]);
                        }
                    }
                    else if (productCampaign.duration * timeBasedDiscountRates[1, 0] < systemTimeDuration)
                    {
                        if (campaignSalesCount < productCampaign.targetSalesCount * timeBasedDiscountRates[1, 0])
                        {
                            price = product.price - (product.price * productCampaign.priceManipulationLimit / 100);
                        }
                        else if (campaignSalesCount == productCampaign.targetSalesCount * timeBasedDiscountRates[1, 0])
                        {
                            price = product.price - (product.price * fixedDiscountRate);
                        }
                        else
                        {
                            price = product.price - (product.price * timeBasedDiscountRates[1, 1]);
                        }
                    }
                    else if (productCampaign.duration * timeBasedDiscountRates[0, 0] < systemTimeDuration)
                    {
                        if (campaignSalesCount < productCampaign.targetSalesCount * timeBasedDiscountRates[0, 0])
                        {
                            price = product.price - (product.price * productCampaign.priceManipulationLimit / 100);
                        }
                        else if (campaignSalesCount == productCampaign.targetSalesCount * timeBasedDiscountRates[0, 0])
                        {
                            price = product.price - (product.price * fixedDiscountRate);
                        }
                        else
                        {
                            price = product.price - (product.price * timeBasedDiscountRates[0, 1]);
                        }
                    }
                    else
                    {
                        price = product.price - (product.price * fixedDiscountRate);
                    }

                    #endregion
                }
            }

            return price;
        }

        /// <summary>
        /// Calculating discount rates based on manipulation limit
        /// </summary>
        /// <param name="manipulationLimit"></param>
        public static void FixDiscountRatesByManipulationLimit(double manipulationLimit)
        {
            fixedDiscountRate = manipulationLimit * 0.50 / 100;
            timeBasedDiscountRates[0, 1] = manipulationLimit * 0.40 / 100;
            timeBasedDiscountRates[1, 1] = manipulationLimit * 0.30 / 100;
            timeBasedDiscountRates[2, 1] = manipulationLimit * 0.20 / 100;
        }

        /// <summary>
        /// Campaign creation process
        /// </summary>
        public static void CreateCampaign()
        {
            Console.WriteLine("Please enter Campaign info:");

            Console.Write("Campaign Name:");
            string campaignName = Console.ReadLine();
            while (campaignList.Any(c => c.name == campaignName))
            {
                Console.Write("Campaign with name {0} already exists. Please enter a new Campaign Name:", campaignName);
                campaignName = Console.ReadLine();
            }

            Console.Write("Product Code:");
            string productCode = Console.ReadLine();
            while (!productList.Any(c => c.code == productCode))
            {
                Console.Write("Product with code {0} does not exists in Products. Please enter an exists Product Code:", productCode);
                productCode = Console.ReadLine();
            }
            while (campaignList.Any(c => c.productCode == productCode && c.duration > systemTimeDuration))
            {
                Console.Write("An active Campaign with product code {0} already exists. Please enter a new Product Code:", productCode);

                productCode = Console.ReadLine();
                while (!productList.Any(c => c.code == productCode))
                {
                    Console.Write("Product with code {0} does not exists in Products. Please enter an exists Product Code:", productCode);
                    productCode = Console.ReadLine();
                }
            }

            Console.Write("Duration:");
            int duration = 0;
            while (!Int32.TryParse(Console.ReadLine(), out duration))
            {
                Console.Write("Please enter a Duration value:");
            };

            Console.Write("Price Manipulation Limit:");
            int priceManipulationLimit = 0;
            while (!Int32.TryParse(Console.ReadLine(), out priceManipulationLimit))
            {
                Console.Write("Please enter a Price Manipulation Limit value:");
            };

            Console.Write("Target Sales Count:");
            int targetSalesCount = 0;
            while (!Int32.TryParse(Console.ReadLine(), out targetSalesCount))
            {
                Console.Write("Please enter a Target Sales Count value:");
            };

            Campaign campaign = new Campaign();
            campaign.name = campaignName;
            campaign.productCode = productCode;
            campaign.duration = duration;
            campaign.priceManipulationLimit = priceManipulationLimit;
            campaign.targetSalesCount = targetSalesCount;
            campaign.createDate = DateTime.Now;
            campaignList.Add(campaign);

            CommonOutput(string.Format("Campaign created; name {0}, product {1}, duration {2}, limit {3}, target sales count {4}", campaignName, productCode, duration, priceManipulationLimit, targetSalesCount));
        }

        /// <summary>
        /// Fetch capmaign information
        /// </summary>
        /// <param name="campaignName">Capmaing name</param>
        public static void GetCampaignInfo(string campaignName)
        {
            var campaign = campaignList.Where(c => c.name == campaignName).FirstOrDefault();
            if (campaign != null)
            {
                string campaignStatus = campaign.duration <= systemTimeDuration ? "Ended" : "Active";

                int totalSales = 0;
                double turnover = 0;
                var ordersOfCurrentCampaign = orderList.Where(o => o.CampaignName == campaignName).ToList();
                foreach (var item in ordersOfCurrentCampaign)
                {
                    totalSales += item.quantity;
                    turnover += (item.quantity * item.price);
                }

                double averageItemPrice = turnover / totalSales;

                CommonOutput(string.Format("Campaign {0} info; Status {1}, Target Sales {2}, Total Sales {3}, Turnover {4}, Average Item Price {5}", campaignName, campaignStatus,
                                campaign.targetSalesCount, totalSales, turnover, averageItemPrice));
            }
            else
            {
                CommonOutput(string.Format("Campaign does not exists with name:{0}", campaignName));
            }
        }

        /// <summary>
        /// Increases system time
        /// </summary>
        public static void IncreaseTime()
        {
            Console.Write("Increase amount:");
            int amount = 0;
            while (!Int32.TryParse(Console.ReadLine(), out amount))
            {
                Console.Write("Please enter a Increase amount value:");
            };

            systemTimeDuration += amount;
            string timeValue = systemTimeDuration.ToString().PadLeft(2, '0') + ":00";
            CommonOutput(string.Format("Time is {0}", timeValue));
        }

        /// <summary>
        /// Prints the sent text to the screen
        /// </summary>
        /// <param name="output"></param>
        public static void CommonOutput(string output)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(output);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
